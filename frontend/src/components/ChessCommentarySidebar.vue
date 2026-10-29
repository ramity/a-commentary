<script setup>
import ChessReactionBadge from './ChessReactionBadge.vue';

const props = defineProps({
  isOpen: Boolean,
  reactions: {
    type: Array,
    default: () => []
  }
});

const emit = defineEmits(['close', 'selectReaction']);

function formatDate(isoStr) {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}
</script>

<template>
  <div>
    <!-- Backdrop -->
    <div
      v-if="isOpen"
      @click="$emit('close')"
      class="fixed inset-0 bg-black/60 backdrop-blur-sm z-40 transition-opacity"
    ></div>

    <!-- Drawer Panel -->
    <aside
      :class="[
        'fixed top-0 right-0 h-full w-96 max-w-[90vw] bg-slate-900 border-l border-slate-800 z-50 flex flex-col shadow-2xl transition-transform duration-300 ease-in-out',
        isOpen ? 'translate-x-0' : 'translate-x-full'
      ]"
    >
      <!-- Header -->
      <div class="p-4 border-b border-slate-800 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="text-xl">♟️</span>
          <div>
            <h2 class="text-sm font-bold text-white">Reader Move Analysis</h2>
            <p class="text-xs text-slate-400">{{ reactions.length }} commentary moves logged</p>
          </div>
        </div>
        <button
          @click="$emit('close')"
          class="text-slate-400 hover:text-white p-1 rounded hover:bg-slate-800 text-lg leading-none"
        >
          &times;
        </button>
      </div>

      <!-- List -->
      <div class="flex-1 overflow-y-auto p-4 space-y-3">
        <div
          v-if="reactions.length === 0"
          class="text-center py-12 text-slate-500 text-xs"
        >
          No move analyses yet.<br />Highlight any text in the post to add one!
        </div>

        <div
          v-for="reaction in reactions"
          :key="reaction.id"
          @click="$emit('selectReaction', reaction)"
          class="bg-slate-950/80 border border-slate-800 hover:border-sky-500/50 rounded-lg p-3 transition-colors cursor-pointer group"
        >
          <div class="flex items-center justify-between mb-2">
            <ChessReactionBadge :classification="reaction.classification" size="sm" />
            <span class="text-[10px] text-slate-500">{{ formatDate(reaction.createdAt) }}</span>
          </div>

          <blockquote class="text-xs text-slate-400 border-l-2 border-slate-700 pl-2.5 my-1.5 italic group-hover:text-slate-300">
            "{{ reaction.highlightedText }}"
          </blockquote>

          <p v-if="reaction.comment" class="text-xs text-slate-200 mt-2 font-medium">
            {{ reaction.comment }}
          </p>

          <div class="mt-2 text-[11px] text-slate-400 flex items-center gap-1.5">
            <span class="w-1.5 h-1.5 rounded-full bg-sky-400"></span>
            <span>{{ reaction.readerName || 'Anonymous' }}</span>
          </div>
        </div>
      </div>
    </aside>
  </div>
</template>
