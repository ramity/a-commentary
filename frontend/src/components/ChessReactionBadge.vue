<script setup>
import { computed } from 'vue';

const props = defineProps({
  classification: {
    type: String,
    required: true
  },
  count: {
    type: Number,
    default: 1
  },
  showLabel: {
    type: Boolean,
    default: true
  },
  size: {
    type: String,
    default: 'md' // 'sm', 'md', 'lg'
  }
});

const config = computed(() => {
  const norm = (props.classification || '').toLowerCase();
  switch (norm) {
    case 'brilliant':
      return { symbol: '‼️', label: 'Brilliant', bg: 'bg-[#1baca6]', text: 'text-white', border: 'border-[#1baca6]' };
    case 'great':
      return { symbol: '❗', label: 'Great', bg: 'bg-[#3692e7]', text: 'text-white', border: 'border-[#3692e7]' };
    case 'best':
      return { symbol: '★', label: 'Best', bg: 'bg-[#7fa650]', text: 'text-white', border: 'border-[#7fa650]' };
    case 'excellent':
      return { symbol: '👍', label: 'Excellent', bg: 'bg-[#98bc64]', text: 'text-white', border: 'border-[#98bc64]' };
    case 'good':
      return { symbol: '✔', label: 'Good', bg: 'bg-[#a7bd86]', text: 'text-white', border: 'border-[#a7bd86]' };
    case 'book':
      return { symbol: '📖', label: 'Book', bg: 'bg-[#a88865]', text: 'text-white', border: 'border-[#a88865]' };
    case 'inaccuracy':
      return { symbol: '⁉️', label: 'Inaccuracy', bg: 'bg-[#e58f2a]', text: 'text-white', border: 'border-[#e58f2a]' };
    case 'mistake':
      return { symbol: '❓', label: 'Mistake', bg: 'bg-[#e6912c]', text: 'text-white', border: 'border-[#e6912c]' };
    case 'blunder':
      return { symbol: '❓❓', label: 'Blunder', bg: 'bg-[#ca3431]', text: 'text-white', border: 'border-[#ca3431]' };
    case 'miss':
      return { symbol: '❌', label: 'Miss', bg: 'bg-[#e06453]', text: 'text-white', border: 'border-[#e06453]' };
    default:
      return { symbol: '💬', label: props.classification, bg: 'bg-slate-700', text: 'text-white', border: 'border-slate-600' };
  }
});
</script>

<template>
  <span
    :class="[
      'inline-flex items-center gap-1 font-bold rounded shadow-sm select-none transition-transform hover:scale-105',
      config.bg,
      config.text,
      size === 'sm' ? 'text-xs px-1.5 py-0.5' : size === 'lg' ? 'text-sm px-3 py-1' : 'text-xs px-2 py-0.5'
    ]"
    :title="`${config.label} move annotation`"
  >
    <span class="font-extrabold tracking-tight">{{ config.symbol }}</span>
    <span v-if="showLabel" class="tracking-wide">{{ config.label }}</span>
    <span v-if="count > 1" class="ml-0.5 opacity-80 text-[10px]">x{{ count }}</span>
  </span>
</template>
