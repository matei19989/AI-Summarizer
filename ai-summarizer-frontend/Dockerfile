# ===================================================================
# Multi-stage Dockerfile for React + Vite Frontend - Optimized
# ===================================================================

# Stage 1: Dependencies
FROM node:20-alpine AS dependencies
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

# Stage 2: Build
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 3: Development runtime
FROM nginx:alpine AS development
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx.dev.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx/ /usr/share/nginx/html/nginx/
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

# Stage 4: Production runtime
FROM nginx:alpine AS production
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx.prod.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx/ /usr/share/nginx/html/nginx/
RUN addgroup -g 1001 -S nginx-user && \
    adduser -u 1001 -S nginx-user -G nginx-user && \
    chown -R nginx-user:nginx-user /usr/share/nginx/html && \
    chown -R nginx-user:nginx-user /var/cache/nginx && \
    chown -R nginx-user:nginx-user /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R nginx-user:nginx-user /var/run/nginx.pid && \
    chmod -R 755 /usr/share/nginx/html
USER nginx-user
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1
CMD ["nginx", "-g", "daemon off;"]

#Stage 5: RUNTIME
FROM production  AS runtime