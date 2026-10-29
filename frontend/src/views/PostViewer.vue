<script setup>
import { ref, onMounted, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../services/api';
import SectionRenderer from '../components/SectionRenderer.vue';
import ChessReactionModal from '../components/ChessReactionModal.vue';
import ChessCommentarySidebar from '../components/ChessCommentarySidebar.vue';

const route = useRoute();
const router = useRouter();

const postId = computed(() => parseInt(route.params.id, 10));
const post = ref(null);
const isLoading = ref(true);
const error = ref(null);

// Reaction popup state
const isReactionModalOpen = ref(false);
const reactionModalPos = ref({ x: 0, y: 0 });
const selectedText = ref('');
const targetSectionId = ref(null);
const startOffset = ref(0);
const endOffset = ref(0);
const isSubmittingReaction = ref(false);

// Commentary sidebar state
const isSidebarOpen = ref(false);

async function loadPost() {
  if (!postId.value) return;
  isLoading.value = true;
  error.value = null;

  try {
    const data = await api.getPost(postId.value);
    post.value = data;
  } catch (err) {
    console.error(err);
    error.value = err.message || 'Failed to load post';
  } finally {
    isLoading.value = false;
  }
}

function handleTextSelected({ sectionId, text, startOffset: start, endOffset: end, rect }) {
  targetSectionId.value = sectionId;
  selectedText.value = text;
  startOffset.value = start;
  endOffset.value = end;

  // Calculate popup position
  const x = Math.max(16, Math.min(window.innerWidth - 340, rect.left));
  const y = rect.bottom + window.scrollY + 8;

  reactionModalPos.value = { x, y };
  isReactionModalOpen.value = true;
}

async function handleReactionSubmit({ classification, readerName, comment }) {
  if (!postId.value || !targetSectionId.value || !selectedText.value) return;

  isSubmittingReaction.value = true;
  try {
    const payload = {
      postSectionId: targetSectionId.value,
      highlightedText: selectedText.value,
      startOffset: startOffset.value,
      endOffset: endOffset.value,
      classification,
      comment,
      readerName
    };

    const newReaction = await api.addReaction(postId.value, payload);
    
    // Add to local reaction list
    if (post.value) {
      if (!post.value.reactions) post.value.reactions = [];
      post.value.reactions.unshift(newReaction);
    }

    isReactionModalOpen.value = false;
    selectedText.value = '';
  } catch (err) {
    alert('Failed to post commentary move: ' + err.message);
  } finally {
    isSubmittingReaction.value = false;
  }
}

function handleSelectReaction(reaction) {
  isSidebarOpen.value = false;
  const sectionEl = document.querySelector(`[data-section-id="${reaction.postSectionId}"]`);
  if (sectionEl) {
    sectionEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
    sectionEl.classList.add('ring-2', 'ring-sky-500');
    setTimeout(() => {
      sectionEl.classList.remove('ring-2', 'ring-sky-500');
    }, 2000);
  }
}

onMounted(() => {
  loadPost();
});

watch(() => route.params.id, () => {
  loadPost();
});
</script>

<template>
  <div class="min-h-screen bg-slate-950 text-slate-100 flex flex-col selection:bg-amber-500/30 selection:text-amber-200">
    <!-- Header -->
    <header class="h-14 border-b border-slate-800 bg-slate-900/80 backdrop-blur px-6 flex items-center justify-between sticky top-0 z-30">
      <div class="flex items-center gap-3">
        <router-link to="/" class="flex items-center gap-2 font-bold text-slate-100 hover:text-sky-400 transition-colors">
          <span class="text-xl">♟️</span>
          <span class="text-sm tracking-wide hidden sm:inline">Stream of Consciousness</span>
        </router-link>
        <span class="text-slate-600">/</span>
        <span class="text-xs text-slate-400 font-mono">Reader View</span>
      </div>

      <div class="flex items-center gap-2">
        <a
          v-if="post && post.id"
          :href="`/posts/${post.id}.html`"
          target="_blank"
          class="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-sky-400 text-xs font-medium border border-slate-700 flex items-center gap-1.5 transition-colors"
          title="Direct fast disk serving via Nginx"
        >
          <span>🚀</span>
          <span class="hidden sm:inline">Static HTML</span>
        </a>

        <router-link
          v-if="post"
          :to="`/editor/${post.id}`"
          class="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition-colors flex items-center gap-1.5"
        >
          <span>✏️</span>
          <span>Edit Draft</span>
        </router-link>

        <button
          @click="isSidebarOpen = true"
          class="px-3 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition-colors flex items-center gap-1.5 shadow"
        >
          <span>♟️ Moves</span>
          <span class="bg-black/20 px-1.5 py-0.5 rounded-full text-[10px]">
            {{ (post && post.reactions) ? post.reactions.length : 0 }}
          </span>
        </button>
      </div>
    </header>

    <!-- Content Body -->
    <main class="flex-1 max-w-3xl w-full mx-auto px-6 py-10 pb-24">
      <div v-if="isLoading" class="text-center py-24 text-slate-500">
        <span class="inline-block animate-spin text-2xl mb-2">⏳</span>
        <p class="text-sm">Loading stream of consciousness post...</p>
      </div>

      <div v-else-if="error" class="bg-rose-950/50 border border-rose-800 rounded-xl p-6 text-center text-rose-300">
        <p class="text-base font-semibold mb-2">Error loading post</p>
        <p class="text-xs text-rose-400 mb-4">{{ error }}</p>
        <router-link to="/" class="text-xs underline text-rose-200">Return to posts directory</router-link>
      </div>

      <article v-else-if="post">
        <!-- Post Header -->
        <header class="border-b border-slate-800 pb-6 mb-8">
          <div class="flex items-center gap-2 mb-3 flex-wrap">
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
            <span v-if="post.rootPostId" class="text-[10px] font-mono px-2 py-0.5 rounded bg-purple-500/10 text-purple-400 border border-purple-500/20">
              Root Branch: #{{ post.rootPostId }}
            </span>
            <span class="text-[10px] text-slate-500 font-mono ml-auto">
              ID: #{{ post.id }}
            </span>
          </div>

          <h1 class="text-3xl sm:text-4xl font-extrabold text-white tracking-tight leading-tight mb-3">
            {{ post.title }}
          </h1>

          <div class="flex items-center gap-4 text-xs text-slate-400">
            <span>By <strong class="text-slate-200">{{ post.author }}</strong></span>
            <span>•</span>
            <span>Created <strong class="text-slate-300">{{ new Date(post.createdTime).toLocaleDateString() }}</strong></span>
            <span v-if="post.reactions && post.reactions.length > 0" class="text-sky-400 font-semibold ml-auto flex items-center gap-1">
              <span>♟️</span>
              <span>{{ post.reactions.length }} reader analyses</span>
            </span>
          </div>

          <!-- Highlight instruction hint -->
          <div class="mt-4 p-2.5 rounded-lg bg-slate-900/80 border border-slate-800/80 text-xs text-slate-400 flex items-center gap-2">
            <span class="text-base">💡</span>
            <span><strong>Tip:</strong> Highlight any sentence or code snippet with your mouse to post a Chess.com move classification (Brilliant, Blunder, Inaccuracy, Best, etc.)!</span>
          </div>
        </header>

        <!-- Sections -->
        <div class="space-y-6">
          <div
            v-for="section in post.sections"
            :key="section.id"
          >
            <SectionRenderer
              :section="section"
              :reactions="post.reactions || []"
              :enable-text-selection="true"
              @text-selected="handleTextSelected"
            />
          </div>
        </div>
      </article>
    </main>

    <!-- Floating Chess.com Reaction Tooltip -->
    <ChessReactionModal
      :visible="isReactionModalOpen"
      :position="reactionModalPos"
      :selected-text="selectedText"
      :is-submitting="isSubmittingReaction"
      @close="isReactionModalOpen = false"
      @submit="handleReactionSubmit"
    />

    <!-- Commentary Sidebar Drawer -->
    <ChessCommentarySidebar
      :is-open="isSidebarOpen"
      :reactions="(post && post.reactions) ? post.reactions : []"
      @close="isSidebarOpen = false"
      @select-reaction="handleSelectReaction"
    />
  </div>
</template>
