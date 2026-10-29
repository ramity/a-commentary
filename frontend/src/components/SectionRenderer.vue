<script setup>
import { computed } from 'vue';
import { marked } from 'marked';
import ChessReactionBadge from './ChessReactionBadge.vue';

const props = defineProps({
  section: {
    type: Object,
    required: true
  },
  reactions: {
    type: Array,
    default: () => []
  },
  enableTextSelection: {
    type: Boolean,
    default: true
  }
});

const emit = defineEmits(['textSelected']);

const sectionReactions = computed(() => {
  return props.reactions.filter(r => r.postSectionId === props.section.id);
});

const renderedContent = computed(() => {
  if (!props.section.content) return '';
  return marked.parse(props.section.content);
});

const renderedPrompt = computed(() => {
  if (!props.section.prompt) return '';
  return marked.parse(props.section.prompt);
});

const renderedOutput = computed(() => {
  if (!props.section.llmOutput) return '';
  return marked.parse(props.section.llmOutput);
});

function handleMouseUp(e) {
  if (!props.enableTextSelection) return;
  const selection = window.getSelection();
  const text = selection ? selection.toString().trim() : '';

  if (text && text.length >= 2) {
    const range = selection.getRangeAt(0);
    const rect = range.getBoundingClientRect();
    emit('textSelected', {
      sectionId: props.section.id,
      text,
      startOffset: range.startOffset,
      endOffset: range.endOffset,
      rect
    });
  }
}
</script>

<template>
  <div
    class="relative group/section my-6 transition-all"
    :data-section-id="section.id"
    @mouseup="handleMouseUp"
  >
    <!-- Reaction badges floating above or inline on this section -->
    <div v-if="sectionReactions.length > 0" class="flex flex-wrap items-center gap-1.5 mb-2.5">
      <ChessReactionBadge
        v-for="reaction in sectionReactions"
        :key="reaction.id"
        :classification="reaction.classification"
        size="sm"
        :title="`${reaction.readerName}: ${reaction.comment}`"
      />
    </div>

    <!-- 1. Markdown Text -->
    <div
      v-if="section.type === 'MarkdownText'"
      class="markdown-body prose prose-invert max-w-none text-slate-200"
      v-html="renderedContent"
    ></div>

    <!-- 2. LLM Conversation Pair -->
    <div
      v-else-if="section.type === 'LLMConversation'"
      class="bg-slate-900/90 border border-slate-800 rounded-2xl p-5 shadow-xl relative overflow-hidden"
    >
      <!-- Model Badge Header -->
      <div class="flex items-center justify-between pb-3 mb-4 border-b border-slate-800/80">
        <div class="flex items-center gap-2">
          <span class="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-md bg-sky-500/10 text-sky-400 border border-sky-500/20">
            <span>🤖</span>
            <span>{{ section.provider || 'AI Pair' }}</span>
          </span>
        </div>
        <a
          v-if="section.shareUrl"
          :href="section.shareUrl"
          target="_blank"
          class="text-xs text-slate-400 hover:text-sky-300 transition-colors flex items-center gap-1 underline underline-offset-4"
        >
          <span>Share Source</span>
          <span>↗</span>
        </a>
      </div>

      <!-- User Prompt Bubble -->
      <div class="ml-auto max-w-[90%] md:max-w-[80%] mb-4">
        <div class="text-[11px] font-bold uppercase tracking-wider text-slate-400 mb-1 flex items-center gap-1.5 justify-end">
          <span>You</span>
          <span>👤</span>
        </div>
        <div
          class="bg-slate-800 border border-slate-700/80 rounded-2xl rounded-tr-sm p-4 text-slate-100 markdown-body text-sm"
          v-html="renderedPrompt"
        ></div>
      </div>

      <!-- LLM Response Bubble -->
      <div class="mr-auto max-w-[95%] md:max-w-[85%]">
        <div class="text-[11px] font-bold uppercase tracking-wider text-sky-400 mb-1 flex items-center gap-1.5">
          <span>🤖</span>
          <span>{{ section.provider || 'AI' }} Response</span>
        </div>
        <div
          class="bg-slate-950/90 border border-sky-500/30 rounded-2xl rounded-tl-sm p-4 text-slate-100 markdown-body text-sm shadow-inner"
          v-html="renderedOutput"
        ></div>
      </div>
    </div>

    <!-- 3. Code Snippet -->
    <div
      v-else-if="section.type === 'CodeSnippet'"
      class="rounded-xl overflow-hidden border border-slate-800 bg-slate-950 shadow-lg"
    >
      <div class="bg-slate-900/90 px-4 py-2 border-b border-slate-800 flex items-center justify-between text-xs text-slate-400 font-mono">
        <span>snippet</span>
        <button
          @click="navigator.clipboard.writeText(section.content)"
          class="hover:text-white transition-colors"
          title="Copy Code"
        >
          Copy
        </button>
      </div>
      <pre class="p-4 text-sm font-mono text-sky-300 overflow-x-auto leading-relaxed"><code>{{ section.content }}</code></pre>
    </div>

    <!-- 4. Quote -->
    <div
      v-else-if="section.type === 'Quote'"
      class="border-l-4 border-sky-500 bg-sky-950/20 pl-5 pr-4 py-3 rounded-r-lg my-4 italic text-slate-200"
      v-html="renderedContent"
    ></div>

    <!-- 5. Image -->
    <div
      v-else-if="section.type === 'Image'"
      class="my-6 text-center"
    >
      <img
        :src="section.content"
        alt="Blog media"
        class="rounded-xl border border-slate-800 max-h-[480px] w-full object-cover mx-auto shadow-xl"
      />
    </div>
  </div>
</template>
