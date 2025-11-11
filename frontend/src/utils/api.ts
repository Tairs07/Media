import axios from 'axios'
import { ElMessage } from 'element-plus'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5219/api',
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
    // 提取错误消息
    let errorMessage = '请求失败，请稍后重试'

    if (error.response) {
      // 服务器返回错误
      if (error.response.data?.error) {
        errorMessage = error.response.data.error.message || error.response.data.error
      } else if (error.response.data?.message) {
        errorMessage = error.response.data.message
      } else if (error.response.statusText) {
        errorMessage = error.response.statusText
      }

      // 特殊状态码处理
      if (error.response.status === 401) {
        errorMessage = '登录已过期，请重新登录'
        localStorage.removeItem('token')
        // 避免循环重定向
        if (window.location.pathname !== '/login') {
          setTimeout(() => {
            window.location.href = '/login'
          }, 1500)
        }
      } else if (error.response.status === 403) {
        errorMessage = '没有权限访问'
      } else if (error.response.status === 404) {
        errorMessage = '请求的资源不存在'
      } else if (error.response.status === 500) {
        errorMessage = '服务器错误，请稍后重试'
      }
    } else if (error.request) {
      // 请求已发出但没有收到响应
      errorMessage = '网络连接失败，请检查网络'
    } else {
      // 其他错误
      errorMessage = error.message || errorMessage
    }

    // 显示错误提示
    ElMessage.error({
      message: errorMessage,
      duration: 3000,
      showClose: true
    })

    // 更新错误消息
    error.message = errorMessage

    return Promise.reject(error)
  }
)

export default api
