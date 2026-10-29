# ========================================================
# Production Dockerfile for Nginx Gateway & Static Host
# Compiles Vue 3 frontend and configures reverse proxy
# ========================================================

# Stage 1: Build Vue 3 Frontend
FROM node:20-alpine AS frontend-builder
WORKDIR /app

# Cache package dependencies
COPY frontend/package*.json ./
RUN npm ci

# Copy frontend source and build
COPY frontend/ ./
RUN npm run build

# Stage 2: Nginx Web Server
FROM nginx:alpine AS runtime
COPY nginx/nginx.conf /etc/nginx/nginx.conf
COPY --from=frontend-builder /app/dist /usr/share/nginx/html

# Create static posts directory for volume mount
RUN mkdir -p /usr/share/nginx/html/posts

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
