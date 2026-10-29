# ==========================================
# Production Dockerfile for Vue 3 SPA Build
# ==========================================

FROM node:20-alpine AS build
WORKDIR /app

# Copy dependency specifications and install
COPY package*.json ./
RUN npm ci

# Copy source code and build production assets
COPY . .
RUN npm run build

# Stage 2: Expose static distribution via lightweight scratch/alpine or direct output
FROM alpine:3.19
WORKDIR /dist
COPY --from=build /app/dist /dist
CMD ["sh", "-c", "echo 'Frontend build artifacts ready' && sleep infinity"]
