import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:5219/api',
  timeout: 10000
})

// 请求拦截器 - 添加 Token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器 - 处理错误
api.interceptors.response.use(
  (response) => {
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      // 避免循环重定向
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    // 统一处理错误消息
    if (error.response?.data?.error) {
      error.message = error.response.data.error.message || error.message
    }
    return Promise.reject(error)
  }
)

export default api
