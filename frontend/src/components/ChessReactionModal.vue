<script setup>
import { ref } from 'vue';

const props = defineProps({
  visible: Boolean,
  position: {
    type: Object,
    default: () => ({ x: 0, y: 0 })
  },
  selectedText: {
    type: String,
    default: ''
  },
  isSubmitting: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['close', 'submit']);

const classifications = [
  { id: 'Brilliant', symbol: '‼️', label: 'Brilliant', color: '#1baca6' },
  { id: 'Great', symbol: '❗', label: 'Great', color: '#3692e7' },
  { id: 'Best', symbol: '★', label: 'Best', color: '#7fa650' },
  { id: 'Excellent', symbol: '👍', label: 'Excl', color: '#98bc64' },
  { id: 'Good', symbol: '✔', label: 'Good', color: '#a7bd86' },
  { id: 'Book', symbol: '📖', label: 'Book', color: '#a88865' },
  { id: 'Inaccuracy', symbol: '⁉️', label: 'Inacc', color: '#e58f2a' },
  { id: 'Mistake', symbol: '❓', label: 'Mistake', color: '#e6912c' },
  { id: 'Blunder', symbol: '❓❓', label: 'Blunder', color: '#ca3431' },
  { id: 'Miss', symbol: '❌', label: 'Miss', color: '#e06453' },
];

const selectedClass = ref('Brilliant');
const readerName = ref('');
const comment = ref('');

function handleSubmit() {
  emit('submit', {
    classification: selectedClass.value,
    readerName: readerName.value.trim() || 'Anonymous Grandmaster',
    comment: comment.value.trim()
  });
  comment.value = '';
}
</script>

<template>
  <div
    v-if="visible"
    class="fixed z-50 w-80 bg-slate-900 border border-slate-700/80 rounded-xl shadow-2xl p-4 text-slate-100 backdrop-blur-md"
    :style="{
      top: `${position.y}px`,
      left: `${position.x}px`
    }"
    @mousedown.stop
  >
    <div class="flex items-center justify-between mb-2 pb-1.5 border-b border-slate-800">
      <span class="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
        <span>♟️</span> Chess.com Move Analysis
      </span>
      <button
        @click="$emit('close')"
        class="text-slate-400 hover:text-white text-base leading-none"
        title="Close"
      >
        &times;
      </button>
    </div>

    <!-- Selected Text Quote snippet -->
    <div class="text-xs text-slate-300 bg-slate-950/70 border-l-2 border-amber-500 px-2 py-1.5 rounded-r mb-3 italic truncate">
      "{{ selectedText }}"
    </div>

    <!-- 10 Move Classification Badges -->
    <div class="grid grid-cols-5 gap-1.5 mb-3">
      <button
        v-for="item in classifications"
        :key="item.id"
        type="button"
        @click="selectedClass = item.id"
        :class="[
          'flex flex-col items-center justify-center p-1.5 rounded-lg border transition-all text-xs font-semibold',
          selectedClass === item.id
            ? 'bg-slate-800 border-sky-400 ring-1 ring-sky-400 scale-105'
            : 'bg-slate-950 border-slate-800 hover:border-slate-600 text-slate-300'
        ]"
      >
        <span class="text-sm mb-0.5" :style="{ color: item.color }">{{ item.symbol }}</span>
        <span class="text-[10px] leading-tight truncate w-full text-center">{{ item.label }}</span>
      </button>
    </div>

    <!-- Inputs -->
    <div class="space-y-2">
      <input
        v-model="readerName"
        type="text"
        placeholder="Your Reader Name (e.g. GM Hikaru)"
        class="w-full bg-slate-950 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-white focus:outline-none focus:border-sky-500 placeholder-slate-500"
      />

      <textarea
        v-model="comment"
        rows="2"
        placeholder="Analyze, critique, or refute this move..."
        class="w-full bg-slate-950 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-white focus:outline-none focus:border-sky-500 placeholder-slate-500 resize-none"
      ></textarea>

      <button
        type="button"
        @click="handleSubmit"
        :disabled="isSubmitting"
        class="w-full bg-sky-600 hover:bg-sky-500 text-white font-semibold py-2 px-3 rounded text-xs transition-colors flex items-center justify-center gap-1.5 shadow"
      >
        <span v-if="isSubmitting">Submitting Move...</span>
        <span v-else>Post Move Commentary ♟️</span>
      </button>
    </div>
  </div>
</template>
