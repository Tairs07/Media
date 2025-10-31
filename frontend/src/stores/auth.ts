import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../utils/api'

interface User {
  id: number
  username: string
  email: string
  avatarUrl?: string
  bio?: string
  createdAt: string
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(localStorage.getItem('token'))

  const login = async (username: string, password: string) => {
    const response = await api.post('/auth/login', { username, password })
    if (response.data.success && response.data.data) {
      const { token: newToken, user: userData } = response.data.data
      token.value = newToken
      user.value = userData
      localStorage.setItem('token', newToken)
      return userData
    } else {
      throw new Error(response.data.error?.message || '登录失败')
    }
  }

  const register = async (username: string, email: string, password: string) => {
    const response = await api.post('/auth/register', { username, email, password })
    if (response.data.success && response.data.data) {
      const { token: newToken, user: userData } = response.data.data
      token.value = newToken
      user.value = userData
      localStorage.setItem('token', newToken)
      return userData
    } else {
      throw new Error(response.data.error?.message || '注册失败')
    }
  }

  const logout = () => {
    user.value = null
    token.value = null
    localStorage.removeItem('token')
  }

  const fetchUser = async () => {
    if (token.value) {
      try {
        const response = await api.get('/auth/me')
        if (response.data.success && response.data.data) {
          user.value = response.data.data
        } else {
          logout()
        }
      } catch (error) {
        logout()
      }
    }
  }

  return {
    user,
    token,
    login,
    register,
    logout,
    fetchUser
  }
})
