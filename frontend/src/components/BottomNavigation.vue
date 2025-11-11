<template>
  <nav class="bottom-navigation" v-if="isMobile">
    <router-link to="/" class="nav-item" :class="{ active: $route.path === '/' }">
      <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <polyline points="9 22 9 12 15 12 15 22" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <span class="nav-label">首页</span>
    </router-link>

    <router-link to="/upload" class="nav-item" :class="{ active: $route.path === '/upload' }" v-if="isLoggedIn">
      <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <polyline points="17 8 12 3 7 8" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <line x1="12" y1="3" x2="12" y2="15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <span class="nav-label">上传</span>
    </router-link>

    <router-link to="/chat" class="nav-item" :class="{ active: $route.path === '/chat' }">
      <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <span class="nav-label">AI对话</span>
    </router-link>

    <router-link to="/profile" class="nav-item" :class="{ active: $route.path === '/profile' }" v-if="isLoggedIn">
      <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <circle cx="12" cy="7" r="4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <span class="nav-label">个人中心</span>
    </router-link>

    <router-link to="/login" class="nav-item" :class="{ active: $route.path === '/login' || $route.path === '/register' }" v-else>
      <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <polyline points="10 17 15 12 10 7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <line x1="15" y1="12" x2="3" y2="12" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <span class="nav-label">登录</span>
    </router-link>
  </nav>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const isLoggedIn = computed(() => authStore.isAuthenticated)

const isMobile = ref(false)

const checkMobile = () => {
  isMobile.value = window.innerWidth < 768
}

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
})

onUnmounted(() => {
  window.removeEventListener('resize', checkMobile)
})
</script>

<style scoped>
.bottom-navigation {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  height: 60px;
  background: rgba(15, 15, 15, 0.95);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  display: flex;
  justify-content: space-around;
  align-items: center;
  padding: 0 1rem;
  z-index: 1000;
  box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.3);
}

.nav-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  text-decoration: none;
  color: rgba(255, 255, 255, 0.6);
  transition: all 0.3s ease;
  padding: 8px 16px;
  border-radius: 12px;
  min-width: 60px;
  min-height: 44px;
  position: relative;
}

.nav-item:active {
  transform: scale(0.95);
}

.nav-item.active {
  color: var(--accent-green, rgb(30, 160, 63));
}

.nav-item.active::before {
  content: '';
  position: absolute;
  top: -1px;
  left: 50%;
  transform: translateX(-50%);
  width: 30px;
  height: 3px;
  background: var(--accent-green, rgb(30, 160, 63));
  border-radius: 0 0 3px 3px;
}

.nav-icon {
  width: 24px;
  height: 24px;
  stroke-width: 2;
}

.nav-label {
  font-size: 11px;
  font-weight: 500;
  white-space: nowrap;
}

.nav-placeholder {
  visibility: hidden;
  pointer-events: none;
}

/* 为页面内容预留底部空间 */
:global(body.has-bottom-nav) {
  padding-bottom: 60px;
}
</style>
