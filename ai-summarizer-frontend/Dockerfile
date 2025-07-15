# ===================================================================
# Updated Dockerfile with DRY Nginx Configuration
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

# Stage 3: Common nginx setup
FROM nginx:alpine AS nginx-base
# Create directory for common nginx configs
RUN mkdir -p /etc/nginx/common

# Copy common configuration files
COPY nginx/common/security-headers.conf /etc/nginx/common/
COPY nginx/common/react-app.conf /etc/nginx/common/
COPY nginx/common/api-proxy.conf /etc/nginx/common/

# Copy built application
COPY --from=build /app/dist /usr/share/nginx/html

# Stage 4: Test runtime
FROM nginx-base AS test
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx.test.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

# Stage 5: Development runtime
FROM nginx-base AS development
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx.dev.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

# Stage 6: Production runtime
FROM nginx-base AS production
RUN rm -rf /etc/nginx/conf.d/*
COPY nginx.prod.conf /etc/nginx/conf.d/default.conf

# Copy error pages
COPY nginx/error-pages/429.html /usr/share/nginx/html/
COPY nginx/error-pages/50x.html /usr/share/nginx/html/

# Security setup
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

# Stage 7: Runtime (default)
FROM production AS runtime