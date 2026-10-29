import { createRouter, createWebHistory } from 'vue-router';
import PostsListView from '../views/PostsListView.vue';
import EditorView from '../views/EditorView.vue';
import PostViewer from '../views/PostViewer.vue';

const routes = [
  {
    path: '/',
    name: 'Home',
    component: PostsListView
  },
  {
    path: '/editor/:id?',
    name: 'Editor',
    component: EditorView
  },
  {
    path: '/posts/:id',
    name: 'PostViewer',
    component: PostViewer
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

export default router;
