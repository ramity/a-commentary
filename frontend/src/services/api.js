const BASE_URL = '/api';

export const api = {
  // Posts
  async getPosts() {
    const res = await fetch(`${BASE_URL}/posts`);
    if (!res.ok) throw new Error('Failed to fetch posts');
    return res.json();
  },

  async getPost(id) {
    const res = await fetch(`${BASE_URL}/posts/${id}`);
    if (!res.ok) throw new Error(`Failed to fetch post ${id}`);
    return res.json();
  },

  async getVersions(id) {
    const res = await fetch(`${BASE_URL}/posts/${id}/versions`);
    if (!res.ok) throw new Error(`Failed to fetch versions for post ${id}`);
    return res.json();
  },

  async createPost(postData) {
    const res = await fetch(`${BASE_URL}/posts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(postData),
    });
    if (!res.ok) throw new Error('Failed to create post');
    return res.json();
  },

  async updatePost(id, postData) {
    const res = await fetch(`${BASE_URL}/posts/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(postData),
    });
    if (!res.ok) throw new Error(`Failed to update post ${id}`);
    return res.json();
  },

  async restoreVersion(id, versionId) {
    const res = await fetch(`${BASE_URL}/posts/${id}/restore/${versionId}`, {
      method: 'POST',
    });
    if (!res.ok) throw new Error(`Failed to restore version ${versionId}`);
    return res.json();
  },

  async compilePost(id) {
    const res = await fetch(`${BASE_URL}/posts/${id}/compile`, {
      method: 'POST',
    });
    if (!res.ok) throw new Error(`Failed to compile post ${id}`);
    return res.json();
  },

  async addReaction(id, reactionData) {
    const res = await fetch(`${BASE_URL}/posts/${id}/reactions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(reactionData),
    });
    if (!res.ok) throw new Error('Failed to submit reaction');
    return res.json();
  },

  // LLM Share Importer
  async importLlmShare(url) {
    const res = await fetch(`${BASE_URL}/posts/import-llm-share`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ url }),
    });
    if (!res.ok) throw new Error('Failed to import LLM share');
    return res.json();
  }
};
