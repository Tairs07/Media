<template>
  <div class="ai-chat-container">
    <!-- 背景动画层 -->
    <div class="background-layer">
      <Orb
        :hue="180"
        :hoverIntensity="0.3"
        :rotateOnHover="true"
        :forceHoverState="false"
        style="width: 100%; height: 100%;"
      />
    </div>

    <!-- 主内容区 -->
    <div class="chat-content">
      <!-- 侧边栏 -->
      <div class="chat-sidebar" :class="{ 'sidebar-open': sidebarOpen }">
        <div class="sidebar-header">
          <button class="new-chat-btn" @click="handleNewChat">
            <span class="btn-icon">+</span>
            新对话
          </button>
        </div>

        <div class="session-list">
          <div
            v-for="session in chatStore.sessions"
            :key="session.id"
            class="session-item"
            :class="{ active: session.id === chatStore.currentSessionId }"
            @click="handleSelectSession(session.id)"
          >
            <div class="session-info">
              <div class="session-title">{{ session.title }}</div>
              <div class="session-meta">
                {{ session.messageCount || 0 }} 条消息
              </div>
            </div>
            <button class="delete-btn" @click.stop="handleDeleteSession(session.id)">
              <el-icon><Delete /></el-icon>
            </button>
          </div>

          <div v-if="chatStore.sessions.length === 0" class="empty-sessions">
            <p>暂无对话</p>
          </div>
        </div>
      </div>

      <!-- 主对话区 -->
      <div class="chat-main">
        <!-- 顶部工具栏 -->
        <div class="chat-header">
          <button class="sidebar-toggle" @click="sidebarOpen = !sidebarOpen">
            <el-icon><Menu /></el-icon>
          </button>

          <div v-if="chatStore.currentSession" class="header-info">
            <h2 class="session-title">{{ chatStore.currentSession.title }}</h2>
            <el-dropdown @command="handleModelChange" trigger="click">
              <span class="model-selector-dropdown">
                <span class="model-name">{{ getModelDisplayName(selectedModel) }}</span>
                <el-icon class="dropdown-icon"><ArrowDown /></el-icon>
              </span>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item
                    v-for="model in chatStore.availableModels"
                    :key="model.name"
                    :command="model.name"
                    :class="{ 'is-selected': selectedModel === model.name }"
                  >
                    {{ model.displayName }}
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>

          <!-- 移动端会话选择器 -->
          <div class="mobile-session-selector">
            <button class="new-chat-mobile" @click="handleNewChat">
              <el-icon><Plus /></el-icon>
            </button>
            <el-select
              v-model="chatStore.currentSessionId"
              placeholder="选择会话"
              size="small"
              class="session-selector-mobile"
              @change="(val) => handleSelectSession(val as number)"
            >
              <el-option
                v-for="session in chatStore.sessions"
                :key="session.id"
                :label="session.title"
                :value="session.id"
              />
            </el-select>
            <el-dropdown @command="handleModelChange" trigger="click">
              <span class="model-selector-mobile-dropdown">
                <span class="model-name-mobile">{{ getModelDisplayName(selectedModel, true) }}</span>
                <el-icon class="dropdown-icon-mobile"><ArrowDown /></el-icon>
              </span>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item
                    v-for="model in chatStore.availableModels"
                    :key="model.name"
                    :command="model.name"
                    :class="{ 'is-selected': selectedModel === model.name }"
                  >
                    {{ model.displayName }}
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>

        <!-- 消息列表 -->
        <div class="message-container" ref="messageContainer">
          <div v-if="!chatStore.currentSession" class="empty-state">
            <FadeContent :duration="1000">
              <div class="empty-content">
                <h1>
                  <GradientText :text="'通义千问 AI 对话'" :colors="['#00ff88', '#00bfff', '#ff00ff']" />
                </h1>
                <p>选择或创建一个对话开始聊天</p>
                <button class="start-btn" @click="handleNewChat">
                  开始对话
                </button>
              </div>
            </FadeContent>
          </div>

          <div v-else class="message-list">
            <div
              v-for="(message, index) in chatStore.currentMessages"
              :key="message.id"
              class="message-item"
              :class="`message-${message.role}`"
              v-show="!(message.role === 'assistant' && !message.content && chatStore.isStreaming)"
            >
              <div class="message-avatar">
                <span v-if="message.role === 'user'">🐷</span>
                <img v-else :src="AIAvatar" alt="AI" class="avatar-image" />
              </div>
              <div class="message-content">
                <div class="message-role">
                  {{ message.role === 'user' ? '你' : 'AI' }}
                </div>
                <div class="message-text">
                  <div v-if="message.role === 'user'" v-html="escapeHtml(message.content)"></div>
                  <div v-else v-html="renderMarkdown(message.content)"></div>
                </div>
                <div class="message-meta">
                  {{ formatTime(message.createdAt) }}
                  <span v-if="message.tokenCount"> · {{ message.tokenCount }} tokens</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 输入区 -->
        <div v-if="chatStore.currentSession" class="input-container">
          <div class="input-wrapper">
            <el-input
              v-model="inputMessage"
              type="textarea"
              :rows="3"
              placeholder="输入消息... (Shift+Enter 换行, Enter 发送)"
              :disabled="chatStore.isStreaming"
              @keydown.enter.exact.prevent="handleSend"
              @keydown.enter.shift.exact="handleNewLine"
              class="message-input"
            />
            <button
              class="send-btn"
              :disabled="!inputMessage.trim() || chatStore.isStreaming"
              @click="handleSend"
            >
              <el-icon v-if="!chatStore.isStreaming"><Promotion /></el-icon>
              <el-icon v-else class="is-loading"><Loading /></el-icon>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Delete, Menu, Promotion, Loading, Plus, ArrowDown } from '@element-plus/icons-vue'
import { useChatStore } from '../stores/chat'
import FadeContent from '../bits-content/Animations/FadeContent/FadeContent.vue'
import GradientText from '../bits-content/TextAnimations/GradientText/GradientText.vue'
import Orb from '../bits-content/Backgrounds/Orb/Orb.vue'
import MarkdownIt from 'markdown-it'
import hljs from 'highlight.js'
import 'highlight.js/styles/github-dark.css'
import AIAvatar from '../../images/AI.gif'

const chatStore = useChatStore()
const inputMessage = ref('')
const selectedModel = ref('qwen-plus')
const sidebarOpen = ref(true)
const messageContainer = ref<HTMLElement>()

// 初始化Markdown渲染器
const md = new MarkdownIt({
  html: false,
  linkify: true,
  typographer: true,
  highlight: function (str, lang) {
    if (lang && hljs.getLanguage(lang)) {
      try {
        return `<pre class="hljs"><code>${hljs.highlight(str, { language: lang, ignoreIllegals: true }).value}</code></pre>`
      } catch (__) {}
    }
    return `<pre class="hljs"><code>${md.utils.escapeHtml(str)}</code></pre>`
  }
})

// 渲染Markdown
const renderMarkdown = (content: string) => {
  return md.render(content)
}

// 转义HTML（用户消息）
const escapeHtml = (text: string) => {
  return text.replace(/[&<>"']/g, (match) => {
    const escapeMap: Record<string, string> = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;'
    }
    return escapeMap[match]
  }).replace(/\n/g, '<br/>')
}

// 格式化时间
const formatTime = (dateString: string) => {
  const date = new Date(dateString)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const minutes = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)
  const days = Math.floor(diff / 86400000)

  if (minutes < 1) return '刚刚'
  if (minutes < 60) return `${minutes}分钟前`
  if (hours < 24) return `${hours}小时前`
  if (days < 7) return `${days}天前`
  
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 滚动到底部
const scrollToBottom = () => {
  nextTick(() => {
    if (messageContainer.value) {
      messageContainer.value.scrollTop = messageContainer.value.scrollHeight
    }
  })
}

// 新建对话
const handleNewChat = async () => {
  try {
    await chatStore.createSession()
    ElMessage.success('创建新对话成功')
  } catch (error) {
    ElMessage.error('创建对话失败')
  }
}

// 选择会话
const handleSelectSession = async (sessionId: number) => {
  chatStore.setCurrentSession(sessionId)
  selectedModel.value = chatStore.currentSession?.model || 'qwen-plus'
  scrollToBottom()
}

// 删除会话
const handleDeleteSession = async (sessionId: number) => {
  try {
    await ElMessageBox.confirm('确定要删除这个对话吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    await chatStore.deleteSession(sessionId)
    ElMessage.success('删除成功')
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 发送消息
const handleSend = async () => {
  if (!inputMessage.value.trim() || !chatStore.currentSessionId) return
  
  const content = inputMessage.value.trim()
  inputMessage.value = ''
  
  try {
    scrollToBottom()
    await chatStore.sendMessage(chatStore.currentSessionId, content, selectedModel.value)
    scrollToBottom()
  } catch (error) {
    ElMessage.error('发送消息失败')
  }
}

// 换行
const handleNewLine = (event: KeyboardEvent) => {
  const target = event.target as HTMLTextAreaElement
  const start = target.selectionStart
  const end = target.selectionEnd
  inputMessage.value = inputMessage.value.substring(0, start) + '\n' + inputMessage.value.substring(end)

  nextTick(() => {
    target.selectionStart = target.selectionEnd = start + 1
  })
}

// 切换模型
const handleModelChange = (modelName: string) => {
  selectedModel.value = modelName
}

// 获取模型显示名称
const getModelDisplayName = (modelName: string, shortName: boolean = false) => {
  const model = chatStore.availableModels.find(m => m.name === modelName)
  if (!model) return modelName

  // 移动端显示简短名称
  if (shortName) {
    const nameMap: Record<string, string> = {
      'qwen-plus': 'Plus',
      'qwen-turbo': 'Turbo',
      'qwen-max': 'Max'
    }
    return nameMap[modelName] || model.displayName
  }

  return model.displayName
}

// 监听消息变化，自动滚动
watch(() => chatStore.currentMessages.length, () => {
  scrollToBottom()
})

// 初始化
onMounted(async () => {
  try {
    await Promise.all([
      chatStore.fetchSessions(),
      chatStore.fetchAvailableModels()
    ])
    
    // 如果有会话，选中第一个
    if (chatStore.sessions.length > 0 && !chatStore.currentSessionId) {
      chatStore.setCurrentSession(chatStore.sessions[0].id)
      selectedModel.value = chatStore.sessions[0].model
    }
  } catch (error) {
    console.error('初始化失败:', error)
  }
})
</script>

<style scoped>
.ai-chat-container {
  position: relative;
  width: 100%;
  height: calc(100vh - 90px); /* 减去导航栏高度90px */
  overflow: hidden;
  background: var(--bg-primary);
}

.background-layer {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 0;
  pointer-events: none;
}

.chat-content {
  position: relative;
  z-index: 1;
  display: flex;
  width: 100%;
  height: 100%;
}

/* 侧边栏 */
.chat-sidebar {
  width: 280px;
  height: 100%;
  background: rgba(18, 18, 18, 0.95);
  backdrop-filter: blur(20px);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  transition: transform 0.3s ease;
}

.sidebar-header {
  padding: 1rem;
  border-bottom: 1px solid var(--border-color);
}

.new-chat-btn {
  width: 100%;
  padding: 0.75rem 1rem;
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue));
  border: none;
  border-radius: 8px;
  color: white;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  transition: all 0.3s ease;
}

.new-chat-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 20px rgba(0, 255, 136, 0.3);
}

.btn-icon {
  font-size: 1.5rem;
  line-height: 1;
}

.session-list {
  flex: 1;
  overflow-y: auto;
  padding: 0.5rem;
}

.session-item {
  padding: 0.875rem;
  margin-bottom: 0.5rem;
  background: rgba(30, 30, 30, 0.6);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.session-item:hover {
  background: rgba(40, 40, 40, 0.8);
  border-color: var(--accent-green);
  transform: translateX(4px);
}

.session-item.active {
  background: rgba(0, 255, 136, 0.1);
  border-color: var(--accent-green);
}

.session-info {
  flex: 1;
  min-width: 0;
}

.session-title {
  color: var(--text-primary);
  font-weight: 500;
  margin-bottom: 0.25rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.session-meta {
  color: var(--text-secondary);
  font-size: 0.75rem;
}

.delete-btn {
  padding: 0.25rem;
  background: transparent;
  border: none;
  color: var(--text-secondary);
  cursor: pointer;
  opacity: 0;
  transition: all 0.2s ease;
}

.session-item:hover .delete-btn {
  opacity: 1;
}

.delete-btn:hover {
  color: #ff4444;
}

.empty-sessions {
  text-align: center;
  padding: 2rem 1rem;
  color: var(--text-secondary);
}

/* 主对话区 */
.chat-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  height: 100%;
  background: rgba(11, 11, 11, 0.7);
  backdrop-filter: blur(15px);
}

.chat-header {
  padding: 1rem 1.5rem;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: rgba(18, 18, 18, 0.8);
}

.sidebar-toggle {
  padding: 0.5rem;
  background: transparent;
  border: 1px solid var(--border-color);
  border-radius: 6px;
  color: var(--text-primary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.sidebar-toggle:hover {
  border-color: var(--accent-green);
  background: rgba(0, 255, 136, 0.1);
}

.header-info {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-left: 1rem;
}

.session-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 400px;
}

/* 模型选择器下拉按钮样式 - 桌面端 */
.model-selector-dropdown {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.2rem;
  height: 45px;
  border: 1px solid rgba(0, 255, 136, 0.3);
  background: rgba(26, 26, 26, 0.95);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: #e0e0e0;
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3), 0 0 8px rgba(0, 255, 136, 0.1);
  min-width: 180px;
  justify-content: space-between;
}

.model-selector-dropdown:hover {
  border-color: rgba(0, 255, 136, 0.5);
  background: rgba(30, 30, 30, 0.95);
  box-shadow: 0 0 16px rgba(0, 255, 136, 0.25), 0 4px 16px rgba(0, 0, 0, 0.3);
  transform: translateY(-1px);
}

.model-name {
  font-weight: 500;
  font-size: 0.95rem;
}

.dropdown-icon {
  color: rgba(0, 255, 136, 0.7);
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  filter: drop-shadow(0 0 2px rgba(0, 255, 136, 0.4));
}

/* 会话选择器样式优化 - 移动端 */
.session-selector-mobile :deep(.el-input__wrapper) {
  background: rgba(26, 26, 26, 0.95) !important;
  border: 1px solid rgba(0, 255, 136, 0.3) !important;
  border-radius: 8px !important;
  backdrop-filter: blur(15px) !important;
  -webkit-backdrop-filter: blur(15px) !important;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3), 0 0 8px rgba(0, 255, 136, 0.1) !important;
  padding: 10px 16px !important;
  min-height: auto !important;
}

.session-selector-mobile :deep(.el-input__wrapper:hover) {
  border-color: rgba(0, 255, 136, 0.5) !important;
  background: rgba(30, 30, 30, 0.95) !important;
  box-shadow: 0 0 16px rgba(0, 255, 136, 0.25), 0 4px 16px rgba(0, 0, 0, 0.3) !important;
  transform: translateY(-1px);
}

.session-selector-mobile :deep(.el-input__wrapper.is-focus) {
  border-color: rgba(0, 255, 136, 0.6) !important;
  background: rgba(35, 35, 35, 0.95) !important;
  box-shadow: 0 0 0 2px rgba(0, 255, 136, 0.2), 0 0 20px rgba(0, 255, 136, 0.35), 0 6px 20px rgba(0, 0, 0, 0.4) !important;
}

.session-selector-mobile :deep(.el-input__inner) {
  color: #e0e0e0 !important;
  font-weight: 500 !important;
  font-size: 0.95rem !important;
  text-align: center !important;
  line-height: 1.2 !important;
}

.session-selector-mobile :deep(.el-input__inner::placeholder) {
  color: rgba(255, 255, 255, 0.35) !important;
  font-weight: 400;
  text-align: center;
}

.session-selector-mobile :deep(.el-select__caret) {
  color: rgba(0, 255, 136, 0.7) !important;
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), color 0.3s ease !important;
  font-size: 14px;
  filter: drop-shadow(0 0 2px rgba(0, 255, 136, 0.4));
}

.session-selector-mobile :deep(.el-select__caret.is-reverse) {
  transform: rotate(180deg);
  color: rgba(0, 191, 255, 0.7) !important;
}

/* AI 对话页面下拉菜单统一样式 - 使用 :deep() 穿透 */
:deep(.el-dropdown-menu) {
  background: rgba(26, 26, 26, 0.98) !important;
  border: 1px solid rgba(0, 255, 136, 0.3) !important;
  border-radius: 8px !important;
  backdrop-filter: blur(20px) !important;
  -webkit-backdrop-filter: blur(20px) !important;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5), 0 0 24px rgba(0, 255, 136, 0.25) !important;
  padding: 8px !important;
}

:deep(.el-dropdown-menu__item) {
  background: transparent !important;
  color: #e0e0e0 !important;
  font-weight: 500 !important;
  padding: 10px 16px !important;
  border-radius: 8px !important;
  margin-bottom: 4px !important;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

:deep(.el-dropdown-menu__item:last-child) {
  margin-bottom: 0 !important;
}

:deep(.el-dropdown-menu__item:hover) {
  background: rgba(0, 255, 136, 0.15) !important;
  color: var(--accent-green-light) !important;
  transform: translateX(4px);
}

:deep(.el-dropdown-menu__item.is-selected) {
  background: linear-gradient(135deg, rgba(0, 255, 136, 0.2), rgba(0, 191, 255, 0.2)) !important;
  color: var(--accent-green-light) !important;
  font-weight: 500 !important;
  border-left: 3px solid var(--accent-green-light) !important;
  position: relative !important;
}

:deep(.el-dropdown-menu__item.is-selected::after) {
  content: '✓';
  position: absolute;
  right: 16px;
  color: var(--accent-green-light);
  font-weight: bold;
}

/* el-select 下拉框样式 */
:deep(.el-select-dropdown) {
  background: rgba(26, 26, 26, 0.98) !important;
  border: 1px solid rgba(0, 255, 136, 0.3) !important;
  border-radius: 8px !important;
  backdrop-filter: blur(20px) !important;
  -webkit-backdrop-filter: blur(20px) !important;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5), 0 0 24px rgba(0, 255, 136, 0.25) !important;
  padding: 8px !important;
}

:deep(.el-select-dropdown__list) {
  padding: 0 !important;
  background: transparent !important;
}

:deep(.el-select-dropdown__item) {
  background: transparent !important;
  color: #e0e0e0 !important;
  font-weight: 500 !important;
  padding: 10px 16px !important;
  border-radius: 8px !important;
  margin-bottom: 4px !important;
  text-align: center !important;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

:deep(.el-select-dropdown__item:last-child) {
  margin-bottom: 0 !important;
}

:deep(.el-select-dropdown__item:hover),
:deep(.el-select-dropdown__item.hover) {
  background: rgba(0, 255, 136, 0.15) !important;
  color: var(--accent-green-light) !important;
  transform: translateX(4px);
}

:deep(.el-select-dropdown__item.is-selected) {
  background: linear-gradient(135deg, rgba(0, 255, 136, 0.2), rgba(0, 191, 255, 0.2)) !important;
  color: var(--accent-green-light) !important;
  font-weight: 500 !important;
  border-left: 3px solid var(--accent-green-light) !important;
  position: relative !important;
}

:deep(.el-select-dropdown__item.is-selected::after) {
  content: '✓';
  position: absolute;
  right: 16px;
  color: var(--accent-green-light);
  font-weight: bold;
}

/* 消息区域 */
.message-container {
  flex: 1;
  overflow-y: auto;
  padding: 2rem;
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.empty-content {
  text-align: center;
  max-width: 500px;
}

.empty-content h1 {
  font-size: 2.5rem;
  margin-bottom: 1rem;
}

.empty-content p {
  color: var(--text-secondary);
  font-size: 1.125rem;
  margin-bottom: 2rem;
}

.start-btn {
  padding: 0.875rem 2rem;
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue));
  border: none;
  border-radius: 50px;
  color: white;
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.3s ease;
}

.start-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0, 255, 136, 0.3);
}

.message-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.message-item {
  display: flex;
  gap: 1rem;
  animation: fadeInUp 0.3s ease;
}

.message-item.message-user {
  flex-direction: row-reverse;
  justify-content: flex-start;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.message-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--bg-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  flex-shrink: 0;
  overflow: hidden;
}

.message-avatar .avatar-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.message-content {
  max-width: fit-content;
  min-width: 100px;
}

.message-user .message-content {
  max-width: fit-content;
  min-width: 100px;
}

.message-assistant .message-content {
  max-width: 80%;
  min-width: 150px;
}

.message-role {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--accent-green);
  margin-bottom: 0.5rem;
}

.message-user .message-role {
  color: var(--accent-blue);
  text-align: right;
}

.message-user .message-text {
  background: rgba(0, 191, 255, 0.15);
  padding: 0.875rem 1.25rem;
  border-radius: 18px 18px 4px 18px;
  border: 1px solid rgba(0, 191, 255, 0.3);
  display: inline-block;
  max-width: 600px;
  word-break: break-word;
}

.message-assistant .message-text {
  background: rgba(30, 30, 30, 0.6);
  padding: 0.875rem 1.25rem;
  border-radius: 18px 18px 18px 4px;
  border: 1px solid rgba(0, 255, 136, 0.15);
  display: inline-block;
  word-break: break-word;
}

.message-text {
  color: var(--text-primary);
  line-height: 1.6;
  word-wrap: break-word;
  width: 100%;
}

.message-text :deep(pre) {
  margin: 0.5rem 0;
  padding: 1rem;
  background: rgba(30, 30, 30, 0.8);
  border-radius: 8px;
  overflow-x: auto;
}

.message-text :deep(code) {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 0.875rem;
}

.message-text :deep(p) {
  margin: 0.5rem 0;
}

.message-meta {
  margin-top: 0.5rem;
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.streaming-indicator {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.streaming-indicator .dot {
  width: 8px;
  height: 8px;
  background: var(--accent-green);
  border-radius: 50%;
  animation: pulse 1.5s ease-in-out infinite;
}

.streaming-indicator .dot:nth-child(2) {
  animation-delay: 0.2s;
}

.streaming-indicator .dot:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes pulse {
  0%, 100% {
    opacity: 0.3;
    transform: scale(0.8);
  }
  50% {
    opacity: 1;
    transform: scale(1.2);
  }
}

/* 输入区域 */
.input-container {
  padding: 1.5rem;
  border-top: 1px solid var(--border-color);
  background: rgba(18, 18, 18, 0.9);
}

.input-wrapper {
  display: flex;
  gap: 1rem;
  align-items: flex-end;
}

.message-input {
  flex: 1;
}

.message-input :deep(.el-textarea__inner) {
  background: rgba(40, 40, 40, 0.8);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #e0e0e0;
  font-size: 0.95rem;
  line-height: 1.6;
  resize: none;
  transition: all 0.3s ease;
}

.message-input :deep(.el-textarea__inner):focus {
  background: rgba(45, 45, 45, 0.9);
  border-color: var(--accent-green);
  box-shadow: 0 0 0 2px rgba(0, 255, 136, 0.1);
}

.message-input :deep(.el-textarea__inner::placeholder) {
  color: rgba(255, 255, 255, 0.3);
}

.send-btn {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue));
  border: none;
  color: white;
  font-size: 1.25rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.send-btn:not(:disabled):hover {
  transform: scale(1.1);
  box-shadow: 0 4px 20px rgba(0, 255, 136, 0.4);
}

.send-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* 移动端会话选择器（默认隐藏） */
.mobile-session-selector {
  display: none;
  gap: 0.75rem;
  align-items: center;
  width: 100%;
}

/* 移动端适配 */
@media (max-width: 768px) {
  .ai-chat-container {
    height: 100vh; /* 占满整个视口 */
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
  }

  .chat-sidebar {
    display: none; /* 移动端完全隐藏侧边栏 */
  }

  .chat-main {
    width: 100%;
  }

  .chat-header {
    padding: 0.875rem 1rem;
    position: sticky;
    top: 0;
    z-index: 20;
    background: rgba(18, 18, 18, 0.98);
    backdrop-filter: blur(25px);
    border-bottom: 1px solid rgba(0, 255, 136, 0.15);
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3);
  }

  /* 隐藏桌面端的会话标题和模型选择器 */
  .header-info {
    display: none !important;
  }

  .sidebar-toggle {
    display: none;
  }

  /* 显示移动端会话选择器 */
  .mobile-session-selector {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    width: 100%;
  }

  .new-chat-mobile {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    background: linear-gradient(135deg, var(--accent-green), var(--accent-blue));
    border: none;
    color: white;
    font-size: 1.4rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    box-shadow: 0 4px 12px rgba(0, 255, 136, 0.3);
    transition: all 0.3s ease;
  }

  .new-chat-mobile:active {
    transform: scale(0.95);
  }

  .session-selector-mobile {
    flex: 1;
    min-width: 0;
  }

  .session-selector-mobile :deep(.el-input__wrapper) {
    height: 44px !important;
    padding: 0 14px !important;
    border-radius: 10px !important;
  }

  .session-selector-mobile :deep(.el-input__inner) {
    font-size: 0.9rem !important;
    text-align: left !important;
    font-weight: 500 !important;
  }

  .model-selector-mobile-dropdown {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0 12px;
    height: 44px;
    border: 1px solid rgba(0, 255, 136, 0.3);
    background: rgba(26, 26, 26, 0.95);
    border-radius: 10px;
    cursor: pointer;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    color: #e0e0e0;
    backdrop-filter: blur(15px);
    -webkit-backdrop-filter: blur(15px);
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3), 0 0 8px rgba(0, 255, 136, 0.1);
    width: 115px;
    flex-shrink: 0;
    justify-content: space-between;
  }

  .model-selector-mobile-dropdown:active {
    transform: scale(0.98);
  }

  .model-name-mobile {
    font-weight: 500;
    font-size: 0.85rem;
  }

  .dropdown-icon-mobile {
    color: rgba(0, 255, 136, 0.7);
    font-size: 12px;
    transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  }

  .message-container {
    padding: 0.75rem;
    padding-top: 60px; /* 为顶部工具栏留空间 */
    padding-bottom: calc(90px + 60px); /* 输入框高度 + 底部导航高度 */
    height: 100vh;
    overflow-y: auto;
  }

  .input-container {
    padding: 0.75rem;
    background: rgba(18, 18, 18, 0.98);
    position: fixed;
    bottom: 60px; /* 固定在底部导航栏上方 */
    left: 0;
    right: 0;
    border-top: 1px solid var(--border-color);
    z-index: 10;
  }

  .input-wrapper {
    max-width: 100%;
  }

  .message-input :deep(.el-textarea__inner) {
    font-size: 0.9rem;
  }

  .send-btn {
    width: 44px;
    height: 44px;
  }

  .empty-content h1 {
    font-size: 1.75rem;
  }

  .empty-content p {
    font-size: 0.95rem;
  }

  .empty-state {
    padding-bottom: calc(90px + 60px);
  }

  .message-avatar {
    width: 32px;
    height: 32px;
    font-size: 1.25rem;
  }

  .message-user .message-text,
  .message-assistant .message-text {
    padding: 0.75rem 1rem;
    font-size: 0.95rem;
  }

  .message-role {
    font-size: 0.8rem;
  }

  .message-meta {
    font-size: 0.7rem;
  }
}
</style>





