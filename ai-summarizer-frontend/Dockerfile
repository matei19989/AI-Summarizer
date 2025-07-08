# ===================================================================
# Multi-stage Dockerfile for React + Vite Frontend - Optimized
# ===================================================================

# Stage 1: Dependencies
FROM node:20-alpine AS dependencies
WORKDIR /app
# Copy package files
COPY package*.json ./
# Install production dependencies
RUN npm ci --only=production

# Stage 2: Build
FROM node:20-alpine AS build
WORKDIR /app
# Copy package files
COPY package*.json ./
# Install ALL dependencies (including dev)
RUN npm ci
# Copy source code
COPY . .
# Build the application
RUN npm run build

# Stage 3: Production runtime
FROM nginx:alpine AS production
# Remove default nginx config
RUN rm -rf /etc/nginx/conf.d/*
# Copy custom nginx config
COPY nginx/nginx.prod.conf /etc/nginx/conf.d/default.conf
# Copy built application
COPY --from=build /app/dist /usr/share/nginx/html
# Add custom error pages
COPY nginx/error-pages/ /usr/share/nginx/html/
# Create non-root user and fix permissions in one layer
RUN addgroup -g 1001 -S nginx-user && \
    adduser -u 1001 -S nginx-user -G nginx-user && \
    chown -R nginx-user:nginx-user /usr/share/nginx/html && \
    chown -R nginx-user:nginx-user /var/cache/nginx && \
    chown -R nginx-user:nginx-user /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R nginx-user:nginx-user /var/run/nginx.pid && \
    chmod -R 755 /usr/share/nginx/html && \
    find /usr/share/nginx/html -type f -exec chmod 644 {} \;
# Switch to non-root user
USER nginx-user
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1
CMD ["nginx", "-g", "daemon off;"]

# Stage 4: Development runtime (optional, for docker-compose override)
FROM nginx:alpine AS development
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx/nginx.dev.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]