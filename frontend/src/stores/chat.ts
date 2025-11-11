import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../utils/api'

export interface ChatSession {
  id: number
  title: string
  model: string
  messageCount?: number
  lastMessageAt?: string
  createdAt: string
  updatedAt: string
}

export interface ChatMessage {
  id: number
  sessionId: number
  role: 'user' | 'assistant'
  content: string
  model?: string
  tokenCount?: number
  createdAt: string
}

export interface QwenModel {
  name: string
  displayName: string
  maxTokens: number
}

export const useChatStore = defineStore('chat', () => {
  // 状态
  const sessions = ref<ChatSession[]>([])
  const currentSessionId = ref<number | null>(null)
  const messages = ref<Record<number, ChatMessage[]>>({})
  const isLoading = ref(false)
  const isStreaming = ref(false)
  const currentStreamContent = ref('')
  const availableModels = ref<QwenModel[]>([])
  
  // 计算属性
  const currentSession = computed(() => {
    if (!currentSessionId.value) return null
    return sessions.value.find(s => s.id === currentSessionId.value) || null
  })
  
  const currentMessages = computed(() => {
    if (!currentSessionId.value) return []
    return messages.value[currentSessionId.value] || []
  })
  
  // 获取会话列表
  const fetchSessions = async (page = 1, pageSize = 20) => {
    try {
      isLoading.value = true
      const response = await api.get('/chat/sessions', {
        params: { page, pageSize }
      })
      
      if (response.data.success) {
        sessions.value = response.data.data.items
      }
    } catch (error) {
      console.error('获取会话列表失败:', error)
      throw error
    } finally {
      isLoading.value = false
    }
  }
  
  // 获取会话详情（包含消息）
  const fetchSessionDetail = async (sessionId: number) => {
    try {
      isLoading.value = true
      const response = await api.get(`/chat/sessions/${sessionId}`)
      
      if (response.data.success) {
        const sessionData = response.data.data
        messages.value[sessionId] = sessionData.messages || []
        
        // 更新会话列表中的对应项
        const sessionIndex = sessions.value.findIndex(s => s.id === sessionId)
        if (sessionIndex >= 0) {
          sessions.value[sessionIndex] = {
            ...sessions.value[sessionIndex],
            ...sessionData
          }
        }
      }
    } catch (error) {
      console.error('获取会话详情失败:', error)
      throw error
    } finally {
      isLoading.value = false
    }
  }
  
  // 创建新会话
  const createSession = async (title?: string, model = 'qwen-plus') => {
    try {
      const response = await api.post('/chat/sessions', { title, model })
      
      if (response.data.success) {
        const newSession = response.data.data
        sessions.value.unshift(newSession)
        currentSessionId.value = newSession.id
        messages.value[newSession.id] = []
        return newSession
      }
    } catch (error) {
      console.error('创建会话失败:', error)
      throw error
    }
  }
  
  // 发送消息（SSE流式）
  const sendMessage = async (sessionId: number, content: string, model?: string) => {
    try {
      isStreaming.value = true
      currentStreamContent.value = ''
      
      // 添加用户消息到本地
      const userMessage: ChatMessage = {
        id: Date.now(), // 临时ID
        sessionId,
        role: 'user',
        content,
        createdAt: new Date().toISOString()
      }
      
      if (!messages.value[sessionId]) {
        messages.value[sessionId] = []
      }
      messages.value[sessionId].push(userMessage)
      
      // 添加AI消息占位符
      const assistantMessage: ChatMessage = {
        id: Date.now() + 1, // 临时ID
        sessionId,
        role: 'assistant',
        content: '',
        model: model || currentSession.value?.model || 'qwen-plus',
        createdAt: new Date().toISOString()
      }
      messages.value[sessionId].push(assistantMessage)
      const assistantIndex = messages.value[sessionId].length - 1
      
      // 构建SSE URL
      const baseURL = api.defaults.baseURL || 'http://localhost:5219/api'
      const token = localStorage.getItem('token')
      
      // 使用fetch进行SSE请求
      const response = await fetch(`${baseURL}/chat/sessions/${sessionId}/messages`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ content, model })
      })
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }
      
      const reader = response.body?.getReader()
      const decoder = new TextDecoder()
      
      if (!reader) {
        throw new Error('无法获取响应流')
      }
      
      let done = false
      while (!done) {
        const { value, done: streamDone } = await reader.read()
        done = streamDone
        
        if (value) {
          const chunk = decoder.decode(value, { stream: true })
          const lines = chunk.split('\n')
          
          for (const line of lines) {
            if (line.startsWith('data:')) {
              try {
                const jsonStr = line.substring(5).trim()
                if (!jsonStr) continue
                
                const data = JSON.parse(jsonStr)

                // 后端返回的字段是PascalCase（首字母大写）
                const eventType = data.Type || data.type

                if (eventType === 'start') {
                  // 更新消息ID
                  assistantMessage.id = data.MessageId || data.messageId
                } else if (eventType === 'content') {
                  // 追加内容
                  const delta = data.Delta || data.delta
                  if (delta) {
                    currentStreamContent.value += delta
                    messages.value[sessionId][assistantIndex].content += delta
                  }
                } else if (eventType === 'done') {
                  // 完成
                  messages.value[sessionId][assistantIndex].id = data.MessageId || data.messageId
                  messages.value[sessionId][assistantIndex].tokenCount = data.TokenCount || data.tokenCount
                  done = true
                } else if (eventType === 'error') {
                  // 错误
                  throw new Error(data.Error || data.error || '发送消息失败')
                } else if (eventType === 'cancelled') {
                  // 取消
                  console.log('消息发送被取消')
                  done = true
                }
              } catch (e) {
                console.error('解析SSE数据失败:', e)
              }
            }
          }
        }
      }
      
      // 刷新会话列表
      await fetchSessions()
    } catch (error) {
      console.error('发送消息失败:', error)
      throw error
    } finally {
      isStreaming.value = false
      currentStreamContent.value = ''
    }
  }
  
  // 更新会话标题
  const updateSessionTitle = async (sessionId: number, title: string) => {
    try {
      const response = await api.put(`/chat/sessions/${sessionId}`, { title })
      
      if (response.data.success) {
        const sessionIndex = sessions.value.findIndex(s => s.id === sessionId)
        if (sessionIndex >= 0) {
          sessions.value[sessionIndex].title = title
        }
      }
    } catch (error) {
      console.error('更新会话标题失败:', error)
      throw error
    }
  }
  
  // 删除会话
  const deleteSession = async (sessionId: number) => {
    try {
      const response = await api.delete(`/chat/sessions/${sessionId}`)
      
      if (response.data.success) {
        sessions.value = sessions.value.filter(s => s.id !== sessionId)
        delete messages.value[sessionId]
        
        if (currentSessionId.value === sessionId) {
          currentSessionId.value = sessions.value[0]?.id || null
        }
      }
    } catch (error) {
      console.error('删除会话失败:', error)
      throw error
    }
  }
  
  // 获取可用模型列表
  const fetchAvailableModels = async () => {
    try {
      const response = await api.get('/chat/models')
      
      if (response.data.success) {
        availableModels.value = response.data.data
      }
    } catch (error) {
      console.error('获取模型列表失败:', error)
      throw error
    }
  }
  
  // 设置当前会话
  const setCurrentSession = (sessionId: number | null) => {
    currentSessionId.value = sessionId
    if (sessionId && !messages.value[sessionId]) {
      fetchSessionDetail(sessionId)
    }
  }
  
  // 清空当前流式内容
  const clearStreamContent = () => {
    currentStreamContent.value = ''
  }
  
  return {
    // 状态
    sessions,
    currentSessionId,
    messages,
    isLoading,
    isStreaming,
    currentStreamContent,
    availableModels,
    
    // 计算属性
    currentSession,
    currentMessages,
    
    // 方法
    fetchSessions,
    fetchSessionDetail,
    createSession,
    sendMessage,
    updateSessionTitle,
    deleteSession,
    fetchAvailableModels,
    setCurrentSession,
    clearStreamContent
  }
})



