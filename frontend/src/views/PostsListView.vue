<script setup>
import { ref, onMounted } from 'vue';
import { api } from '../services/api';

const posts = ref([]);
const isLoading = ref(true);
const error = ref(null);

async function fetchPosts() {
  isLoading.value = true;
  error.value = null;
  try {
    posts.value = await api.getPosts();
  } catch (err) {
    error.value = err.message || 'Failed to load posts';
  } finally {
    isLoading.value = false;
  }
}

onMounted(() => {
  fetchPosts();
});
</script>

<template>
  <div class="min-h-screen bg-slate-950 text-slate-100 flex flex-col">
    <!-- Top Navbar -->
    <header class="h-16 border-b border-slate-800 bg-slate-900/60 backdrop-blur px-6 flex items-center justify-between sticky top-0 z-20">
      <div class="flex items-center gap-3">
        <span class="text-2xl">♟️</span>
        <div>
          <h1 class="text-sm font-bold text-white tracking-wide">Stream of Consciousness</h1>
          <p class="text-[11px] text-slate-400">AI Learning Documentation & Commentary Platform</p>
        </div>
      </div>

      <router-link
        to="/editor"
        class="bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs px-4 py-2 rounded-lg transition-all shadow flex items-center gap-2"
      >
        <span>+</span>
        <span>New Stream of Consciousness</span>
      </router-link>
    </header>

    <!-- Main Content -->
    <main class="max-w-5xl w-full mx-auto px-6 py-10 flex-1">
      <div class="flex items-center justify-between mb-8">
        <div>
          <h2 class="text-2xl font-extrabold text-white tracking-tight">Documented Explorations</h2>
          <p class="text-xs text-slate-400 mt-1">
            Browse stream-of-consciousness entries, version branches, and reader chess move commentaries.
          </p>
        </div>
        <button
          @click="fetchPosts"
          class="text-xs text-slate-400 hover:text-white px-2.5 py-1.5 rounded-lg border border-slate-800 hover:bg-slate-900 transition-colors"
        >
          🔄 Refresh
        </button>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="text-center py-24 text-slate-500">
        <span class="inline-block animate-spin text-2xl mb-2">⏳</span>
        <p class="text-xs">Fetching posts...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="bg-rose-950/40 border border-rose-800 rounded-xl p-6 text-center text-rose-300">
        <p class="text-sm font-semibold mb-2">Error connecting to backend API</p>
        <p class="text-xs text-rose-400">{{ error }}</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="posts.length === 0" class="text-center py-20 bg-slate-900/40 border border-slate-800 rounded-2xl p-8">
        <span class="text-4xl mb-3 block">📝</span>
        <h3 class="text-base font-bold text-white mb-1">No blog posts yet</h3>
        <p class="text-xs text-slate-400 mb-4 max-w-sm mx-auto">
          Start documenting your learning experience and pair programming dialogues with AI models!
        </p>
        <router-link
          to="/editor"
          class="inline-flex items-center gap-1.5 bg-sky-600 hover:bg-sky-500 text-white font-bold text-xs px-4 py-2 rounded-lg transition-colors"
        >
          <span>Create First Post</span>
          <span>→</span>
        </router-link>
      </div>

      <!-- Posts Grid -->
      <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div
          v-for="post in posts"
          :key="post.id"
          class="bg-slate-900/80 border border-slate-800 hover:border-slate-700 rounded-2xl p-5 flex flex-col justify-between transition-all hover:shadow-xl group"
        >
          <div>
            <div class="flex items-center justify-between gap-2 mb-3">
              <span
                :class="[
                  'text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded border',
                  post.isPublished
                    ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
                    : 'bg-amber-500/10 text-amber-400 border-amber-500/20'
                ]"
              >
                {{ post.isPublished ? '● Published' : '○ Draft' }}
              </span>

              <div class="flex items-center gap-2">
                <span v-if="post.rootPostId" class="text-[10px] text-purple-400 font-mono bg-purple-950/40 border border-purple-800/40 px-1.5 py-0.5 rounded">
                  Root #{{ post.rootPostId }}
                </span>
                <span class="text-[10px] text-slate-500 font-mono">
                  #{{ post.id }}
                </span>
              </div>
            </div>

            <router-link
              :to="`/posts/${post.id}`"
              class="text-lg font-bold text-white group-hover:text-sky-300 transition-colors line-clamp-2 leading-snug mb-2"
            >
              {{ post.title }}
            </router-link>

            <div class="flex items-center gap-3 text-xs text-slate-400 mb-4">
              <span>By <strong class="text-slate-300">{{ post.author }}</strong></span>
              <span>•</span>
              <span>{{ new Date(post.updatedTime).toLocaleDateString() }}</span>
            </div>
          </div>

          <div class="pt-4 border-t border-slate-800/80 flex items-center justify-between">
            <div class="flex items-center gap-3 text-xs text-slate-400">
              <span title="Sections">📄 {{ post.sectionCount }}</span>
              <span title="Chess Commentary Reactions" class="text-sky-400 font-semibold">♟️ {{ post.reactionCount }}</span>
            </div>

            <div class="flex items-center gap-2">
              <a
                v-if="post.isPublished"
                :href="`/posts/${post.id}.html`"
                target="_blank"
                class="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-sky-400 text-xs font-medium border border-slate-700 transition-colors"
                title="Direct fast disk serving from Nginx"
              >
                Static
              </a>

              <router-link
                :to="`/editor/${post.id}`"
                class="px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition-colors"
              >
                Edit
              </router-link>

              <router-link
                :to="`/posts/${post.id}`"
                class="px-2.5 py-1 rounded bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold transition-colors"
              >
                Read
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
