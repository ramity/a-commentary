<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { marked } from 'marked';
import { api } from '../services/api';
import SectionRenderer from '../components/SectionRenderer.vue';

const route = useRoute();
const router = useRouter();

const postId = computed(() => route.params.id ? parseInt(route.params.id, 10) : null);
const isLoading = ref(false);
const isSaving = ref(false);
const isImporting = ref(false);
const importUrl = ref('');
const notification = ref(null);

// Post state
const form = reactive({
  id: null,
  title: '',
  author: '',
  createdTime: null,
  updatedTime: null,
  isPublished: false,
  previousVersionId: null,
  nextVersionId: null,
  rootPostId: null,
  sections: []
});

const versions = ref([]);

function showToast(message, type = 'success') {
  notification.value = { message, type };
  setTimeout(() => {
    notification.value = null;
  }, 4000);
}

async function loadPostData(id) {
  if (!id) {
    // Initial blank post template
    form.id = null;
    form.title = 'Stream of Consciousness: Exploring Reasoning Models';
    form.author = 'Alex Mercer';
    form.createdTime = new Date().toISOString();
    form.updatedTime = new Date().toISOString();
    form.isPublished = false;
    form.previousVersionId = null;
    form.nextVersionId = null;
    form.rootPostId = null;
    form.sections = [
      {
        id: null,
        sortOrder: 1,
        type: 'MarkdownText',
        content: '# Thoughts on LLM Latency & CoT\n\nI was writing a streaming pipeline and wondered if we could parse chain-of-thought tokens before the final completion arrived...'
      },
      {
        id: null,
        sortOrder: 2,
        type: 'LLMConversation',
        provider: 'ChatGPT-4o',
        shareUrl: '',
        prompt: 'How does speculative decoding differ from speculative execution in hardware?',
        llmOutput: 'Speculative decoding in LLMs runs a small draft model to generate candidate tokens, which the larger target model verifies in parallel in a single forward pass.\n\nIn hardware, speculative execution predicts branch outcomes and computes instructions ahead of time, discarding state if the branch is mispredicted.'
      }
    ];
    versions.value = [];
    return;
  }

  isLoading.value = true;
  try {
    const post = await api.getPost(id);
    form.id = post.id;
    form.title = post.title;
    form.author = post.author;
    form.createdTime = post.createdTime;
    form.updatedTime = post.updatedTime;
    form.isPublished = post.isPublished;
    form.previousVersionId = post.previousVersionId;
    form.nextVersionId = post.nextVersionId;
    form.rootPostId = post.rootPostId;
    form.sections = (post.sections || []).map(s => ({ ...s }));

    const verList = await api.getVersions(id);
    versions.value = verList;
  } catch (err) {
    console.error(err);
    showToast('Failed to load post: ' + err.message, 'error');
  } finally {
    isLoading.value = false;
  }
}

// Section Management
function addSection(type) {
  const nextSort = form.sections.length > 0 
    ? Math.max(...form.sections.map(s => s.sortOrder || 0)) + 1 
    : 1;

  const newSection = {
    id: null,
    sortOrder: nextSort,
    type,
    content: '',
    prompt: type === 'LLMConversation' ? '' : null,
    llmOutput: type === 'LLMConversation' ? '' : null,
    provider: type === 'LLMConversation' ? 'ChatGPT' : null,
    shareUrl: null
  };

  form.sections.push(newSection);
}

function removeSection(index) {
  form.sections.splice(index, 1);
  reindexSortOrder();
}

function moveSection(index, direction) {
  const targetIndex = index + direction;
  if (targetIndex < 0 || targetIndex >= form.sections.length) return;
  const temp = form.sections[index];
  form.sections[index] = form.sections[targetIndex];
  form.sections[targetIndex] = temp;
  reindexSortOrder();
}

function reindexSortOrder() {
  form.sections.forEach((sec, idx) => {
    sec.sortOrder = idx + 1;
  });
}

// LLM Share Import
async function handleImportShare() {
  if (!importUrl.value.trim()) return;

  isImporting.value = true;
  try {
    const result = await api.importLlmShare(importUrl.value.trim());
    if (result.success && result.messages && result.messages.length > 0) {
      result.messages.forEach(msg => {
        const nextSort = form.sections.length > 0 
          ? Math.max(...form.sections.map(s => s.sortOrder || 0)) + 1 
          : 1;

        form.sections.push({
          id: null,
          sortOrder: nextSort,
          type: 'LLMConversation',
          provider: msg.provider || result.provider || 'Imported AI',
          shareUrl: result.shareUrl,
          prompt: msg.prompt,
          llmOutput: msg.llmOutput,
          content: ''
        });
      });
      showToast(`Imported ${result.messages.length} LLM interaction pairs!`, 'success');
      importUrl.value = '';
    } else {
      showToast(result.errorMessage || 'Failed to extract LLM messages.', 'error');
    }
  } catch (err) {
    console.error(err);
    showToast('Failed to import share: ' + err.message, 'error');
  } finally {
    isImporting.value = false;
  }
}

// Save & Branching
async function handleSave(createBranch = false) {
  isSaving.value = true;
  try {
    const payload = {
      title: form.title,
      author: form.author,
      isPublished: form.isPublished,
      createBranch,
      sections: form.sections.map((s, idx) => ({
        id: s.id,
        sortOrder: idx + 1,
        type: s.type,
        content: s.content || '',
        prompt: s.prompt,
        llmOutput: s.llmOutput,
        provider: s.provider,
        shareUrl: s.shareUrl
      }))
    };

    if (form.id) {
      const res = await api.updatePost(form.id, payload);
      showToast(createBranch ? 'Branch created successfully!' : 'Post saved and updated!', 'success');
      if (createBranch && res.id !== form.id) {
        router.push(`/editor/${res.id}`);
      } else {
        await loadPostData(form.id);
      }
    } else {
      const res = await api.createPost(payload);
      showToast('New post created successfully!', 'success');
      router.push(`/editor/${res.id}`);
    }
  } catch (err) {
    console.error(err);
    showToast('Error saving: ' + err.message, 'error');
  } finally {
    isSaving.value = false;
  }
}

async function handleRestoreVersion(versionId) {
  if (!versionId || !form.id) return;
  if (!confirm(`Are you sure you want to restore content from version #${versionId}?`)) return;

  try {
    isLoading.value = true;
    await api.restoreVersion(form.id, versionId);
    showToast(`Restored content from version #${versionId}`, 'success');
    await loadPostData(form.id);
  } catch (err) {
    showToast('Error restoring version: ' + err.message, 'error');
  } finally {
    isLoading.value = false;
  }
}

async function handleCompile() {
  if (!form.id) return;
  try {
    const res = await api.compilePost(form.id);
    showToast(`Static HTML compiled to ${res.staticUrl}`, 'success');
  } catch (err) {
    showToast('Static compile failed: ' + err.message, 'error');
  }
}

onMounted(() => {
  loadPostData(postId.value);
});

watch(() => route.params.id, (newId) => {
  loadPostData(newId ? parseInt(newId, 10) : null);
});
</script>

<template>
  <div class="h-screen flex flex-col bg-slate-950 text-slate-100 overflow-hidden">
    <!-- Notification Toast -->
    <div
      v-if="notification"
      :class="[
        'fixed top-4 right-4 z-50 px-4 py-2.5 rounded-lg shadow-2xl text-xs font-semibold border flex items-center gap-2 transition-all',
        notification.type === 'error'
          ? 'bg-rose-950 border-rose-700 text-rose-200'
          : 'bg-emerald-950 border-emerald-700 text-emerald-200'
      ]"
    >
      <span>{{ notification.type === 'error' ? '⚠️' : '✅' }}</span>
      <span>{{ notification.message }}</span>
    </div>

    <!-- Top Navigation Header -->
    <header class="h-14 border-b border-slate-800 bg-slate-900/80 backdrop-blur px-4 flex items-center justify-between shrink-0">
      <div class="flex items-center gap-3">
        <router-link to="/" class="flex items-center gap-2 font-bold text-slate-100 hover:text-sky-400 transition-colors">
          <span class="text-xl">♟️</span>
          <span class="text-sm tracking-wide hidden sm:inline">Stream of Consciousness</span>
        </router-link>
        <span class="text-slate-600">/</span>
        <span class="text-xs text-slate-400 font-mono">
          {{ form.id ? `Post #${form.id}` : 'New Post' }}
        </span>
      </div>

      <!-- Action Buttons -->
      <div class="flex items-center gap-2">
        <button
          v-if="form.id"
          type="button"
          @click="handleCompile"
          class="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium border border-slate-700 flex items-center gap-1.5 transition-colors"
          title="Compile static HTML to disk"
        >
          <span>⚡</span>
          <span class="hidden md:inline">Compile Static</span>
        </button>

        <a
          v-if="form.id"
          :href="`/posts/${form.id}.html`"
          target="_blank"
          class="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-sky-400 text-xs font-medium border border-slate-700 flex items-center gap-1.5 transition-colors"
          title="Direct Nginx fast disk serving"
        >
          <span>🚀</span>
          <span class="hidden md:inline">Static File</span>
        </a>

        <router-link
          v-if="form.id"
          :to="`/posts/${form.id}`"
          class="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 flex items-center gap-1.5 transition-colors"
        >
          <span>👁️</span>
          <span class="hidden md:inline">Viewer</span>
        </router-link>

        <button
          v-if="form.id"
          type="button"
          @click="handleSave(true)"
          :disabled="isSaving"
          class="px-3 py-1.5 rounded-lg bg-purple-900/60 hover:bg-purple-800 text-purple-200 text-xs font-semibold border border-purple-700/60 transition-colors flex items-center gap-1.5"
          title="Branch this draft into a new divergence"
        >
          <span>🌿</span>
          <span class="hidden sm:inline">Fork Branch</span>
        </button>

        <button
          type="button"
          @click="handleSave(false)"
          :disabled="isSaving"
          class="px-3.5 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition-all shadow-md flex items-center gap-1.5"
        >
          <span v-if="isSaving">Saving...</span>
          <span v-else>💾 Save Post</span>
        </button>
      </div>
    </header>

    <!-- Main Workspace (Split View) -->
    <div class="flex-1 flex overflow-hidden">
      <!-- LEFT PANE: Form Builder & Section Manager -->
      <div class="w-1/2 h-full flex flex-col border-r border-slate-800 overflow-y-auto p-4 space-y-4">
        <!-- Post Metadata Card -->
        <div class="bg-slate-900/90 border border-slate-800 rounded-xl p-4 space-y-3">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold text-slate-400 uppercase tracking-wider">Post Details</span>
            <div class="flex items-center gap-2">
              <label class="flex items-center gap-1.5 text-xs text-slate-300 cursor-pointer">
                <input
                  v-model="form.isPublished"
                  type="checkbox"
                  class="rounded bg-slate-800 border-slate-700 text-sky-500 focus:ring-sky-500/20"
                />
                <span>Published</span>
              </label>
              <span
                v-if="form.createdTime"
                class="text-[11px] bg-slate-800 text-slate-400 px-2 py-0.5 rounded font-mono"
              >
                {{ new Date(form.createdTime).toLocaleDateString() }}
              </span>
            </div>
          </div>

          <div class="space-y-2">
            <input
              v-model="form.title"
              type="text"
              placeholder="Title of your stream of consciousness..."
              class="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white font-semibold focus:outline-none focus:border-sky-500"
            />
            <div class="flex gap-2">
              <input
                v-model="form.author"
                type="text"
                placeholder="Author name"
                class="w-1/2 bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-sky-500"
              />
              <div v-if="form.rootPostId" class="w-1/2 flex items-center px-3 py-1.5 bg-slate-950/60 border border-slate-800 rounded-lg text-xs text-purple-400 font-mono">
                Root Branch: #{{ form.rootPostId }}
              </div>
            </div>
          </div>

          <!-- Version Control Dropdown Selectors -->
          <div v-if="versions.length > 0" class="pt-2 border-t border-slate-800/80 flex flex-wrap gap-2 items-center text-xs">
            <div class="flex items-center gap-1 text-slate-400">
              <span>🌿 Versions:</span>
              <select
                @change="router.push(`/editor/${$event.target.value}`)"
                :value="form.id"
                class="bg-slate-950 border border-slate-800 text-slate-300 rounded px-2 py-1 text-xs focus:outline-none focus:border-purple-500"
              >
                <option v-for="v in versions" :key="v.id" :value="v.id">
                  #{{ v.id }} - {{ v.title }} {{ v.isCurrent ? '(Active)' : '' }}
                </option>
              </select>
            </div>

            <!-- Restore from previous version button -->
            <button
              v-if="form.previousVersionId"
              type="button"
              @click="handleRestoreVersion(form.previousVersionId)"
              class="text-xs text-amber-400 hover:text-amber-300 underline underline-offset-4 ml-auto"
            >
              Restore from #{{ form.previousVersionId }}
            </button>
          </div>
        </div>

        <!-- Import LLM Share Bar -->
        <div class="bg-gradient-to-r from-sky-950/40 to-slate-900 border border-sky-900/50 rounded-xl p-3">
          <label class="block text-xs font-bold text-sky-300 mb-1 flex items-center gap-1.5">
            <span>⚡</span>
            <span>Import Public LLM Share Link</span>
            <span class="text-[10px] text-sky-400/70 font-normal">(ChatGPT, Claude, Gemini)</span>
          </label>
          <div class="flex gap-2">
            <input
              v-model="importUrl"
              type="url"
              placeholder="https://chatgpt.com/share/... or https://claude.ai/share/..."
              class="flex-1 bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none focus:border-sky-500"
            />
            <button
              type="button"
              @click="handleImportShare"
              :disabled="isImporting"
              class="bg-sky-600 hover:bg-sky-500 text-white font-semibold text-xs px-3 py-1.5 rounded-lg transition-colors flex items-center gap-1 shrink-0"
            >
              <span v-if="isImporting">Fetching...</span>
              <span v-else>Import Chat 🚀</span>
            </button>
          </div>
        </div>

        <!-- Dynamic Section Toolbar -->
        <div class="flex items-center justify-between pt-1">
          <span class="text-xs font-bold text-slate-400 uppercase tracking-wider">Dynamic Sections</span>
          <div class="flex items-center gap-1.5 flex-wrap">
            <button
              type="button"
              @click="addSection('MarkdownText')"
              class="px-2.5 py-1 rounded bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 text-xs transition-colors"
            >
              + Add Text
            </button>
            <button
              type="button"
              @click="addSection('LLMConversation')"
              class="px-2.5 py-1 rounded bg-sky-950/70 hover:bg-sky-900 text-sky-300 border border-sky-800/60 text-xs font-semibold transition-colors"
            >
              + Add LLM Pair
            </button>
            <button
              type="button"
              @click="addSection('CodeSnippet')"
              class="px-2.5 py-1 rounded bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 text-xs transition-colors"
            >
              + Add Code
            </button>
            <button
              type="button"
              @click="addSection('Quote')"
              class="px-2.5 py-1 rounded bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 text-xs transition-colors"
            >
              + Add Quote
            </button>
            <button
              type="button"
              @click="addSection('Image')"
              class="px-2.5 py-1 rounded bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 text-xs transition-colors"
            >
              + Add Image
            </button>
          </div>
        </div>

        <!-- Section Builder List -->
        <div class="space-y-3 pb-8">
          <div
            v-for="(section, idx) in form.sections"
            :key="idx"
            class="bg-slate-900/90 border border-slate-800 rounded-xl p-3.5 space-y-2.5 relative group"
          >
            <!-- Section Header Bar -->
            <div class="flex items-center justify-between text-xs text-slate-400 pb-2 border-b border-slate-800/80">
              <div class="flex items-center gap-2">
                <span class="w-5 h-5 rounded-full bg-slate-800 flex items-center justify-center font-mono text-[10px] text-slate-400">
                  {{ idx + 1 }}
                </span>
                <span class="font-semibold text-slate-300">{{ section.type }}</span>
              </div>

              <!-- Reorder & Delete -->
              <div class="flex items-center gap-1">
                <button
                  type="button"
                  @click="moveSection(idx, -1)"
                  :disabled="idx === 0"
                  class="p-1 hover:text-white disabled:opacity-30"
                  title="Move Up"
                >
                  ↑
                </button>
                <button
                  type="button"
                  @click="moveSection(idx, 1)"
                  :disabled="idx === form.sections.length - 1"
                  class="p-1 hover:text-white disabled:opacity-30"
                  title="Move Down"
                >
                  ↓
                </button>
                <button
                  type="button"
                  @click="removeSection(idx)"
                  class="p-1 text-rose-400 hover:text-rose-300 ml-1"
                  title="Delete Section"
                >
                  🗑️
                </button>
              </div>
            </div>

            <!-- Form Fields based on SectionType -->
            <!-- 1. LLM Pair Form -->
            <div v-if="section.type === 'LLMConversation'" class="space-y-2">
              <div class="flex gap-2">
                <input
                  v-model="section.provider"
                  type="text"
                  placeholder="Provider (e.g. Claude 3.5, ChatGPT-4o, Gemini 1.5)"
                  class="w-1/2 bg-slate-950 border border-slate-800 rounded px-2.5 py-1 text-xs text-sky-300"
                />
                <input
                  v-model="section.shareUrl"
                  type="url"
                  placeholder="Share URL (optional)"
                  class="w-1/2 bg-slate-950 border border-slate-800 rounded px-2.5 py-1 text-xs text-slate-400"
                />
              </div>

              <div>
                <label class="block text-[11px] font-semibold text-slate-400 mb-1">User Prompt</label>
                <textarea
                  v-model="section.prompt"
                  rows="2"
                  placeholder="Enter the prompt sent to the LLM..."
                  class="w-full bg-slate-950 border border-slate-800 rounded-lg p-2 text-xs text-slate-200 focus:outline-none focus:border-sky-500 font-mono"
                ></textarea>
              </div>

              <div>
                <label class="block text-[11px] font-semibold text-sky-400 mb-1">
                  Editable LLM Response <span class="text-slate-500 font-normal">(Fully editable before publishing)</span>
                </label>
                <textarea
                  v-model="section.llmOutput"
                  rows="4"
                  placeholder="Enter or edit the LLM response markdown..."
                  class="w-full bg-slate-950 border border-slate-800 rounded-lg p-2 text-xs text-slate-200 focus:outline-none focus:border-sky-500 font-mono"
                ></textarea>
              </div>
            </div>

            <!-- 2. Code Snippet Form -->
            <div v-else-if="section.type === 'CodeSnippet'">
              <textarea
                v-model="section.content"
                rows="4"
                placeholder="// Enter code snippet here..."
                class="w-full bg-slate-950 border border-slate-800 rounded-lg p-2 text-xs text-sky-300 focus:outline-none focus:border-sky-500 font-mono"
              ></textarea>
            </div>

            <!-- 3. Image URL Form -->
            <div v-else-if="section.type === 'Image'">
              <input
                v-model="section.content"
                type="text"
                placeholder="https://example.com/image.png"
                class="w-full bg-slate-950 border border-slate-800 rounded-lg p-2 text-xs text-slate-200 focus:outline-none focus:border-sky-500"
              />
            </div>

            <!-- 4. Text or Quote Markdown -->
            <div v-else>
              <textarea
                v-model="section.content"
                rows="4"
                :placeholder="section.type === 'Quote' ? 'Enter quote...' : 'Write your stream of consciousness thoughts in Markdown...'"
                class="w-full bg-slate-950 border border-slate-800 rounded-lg p-2 text-xs text-slate-200 focus:outline-none focus:border-sky-500"
              ></textarea>
            </div>
          </div>
        </div>
      </div>

      <!-- RIGHT PANE: Live Render Preview -->
      <div class="w-1/2 h-full flex flex-col bg-slate-950 overflow-y-auto p-6 md:p-8">
        <div class="max-w-2xl mx-auto w-full pb-16">
          <div class="border-b border-slate-800 pb-4 mb-6">
            <div class="flex items-center gap-2 mb-2">
              <span class="text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-sky-500/10 text-sky-400 border border-sky-500/20">
                Live Preview
              </span>
              <span v-if="form.isPublished" class="text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                Published
              </span>
            </div>
            <h1 class="text-3xl font-extrabold text-white tracking-tight leading-tight">
              {{ form.title || 'Untitled Stream of Consciousness' }}
            </h1>
            <div class="flex items-center gap-4 text-xs text-slate-400 mt-2 font-medium">
              <span>By <strong class="text-slate-200">{{ form.author || 'Anonymous' }}</strong></span>
              <span>•</span>
              <span>Updated <strong class="text-slate-200">{{ new Date().toLocaleTimeString() }}</strong></span>
            </div>
          </div>

          <!-- Live Section Previews -->
          <div v-if="form.sections.length === 0" class="text-center py-20 text-slate-600 text-xs">
            No sections yet. Add a section on the left to see live rendering.
          </div>

          <div v-for="(section, idx) in form.sections" :key="idx">
            <SectionRenderer :section="section" :enable-text-selection="false" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
