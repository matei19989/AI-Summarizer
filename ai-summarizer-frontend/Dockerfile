# ===================================================================
# Multi-stage Dockerfile for React + Vite Frontend
# Supports different nginx configurations for dev/prod
# ===================================================================

# Build stage - use Node.js to build the React application
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files first for better layer caching
COPY package*.json ./

# Install ALL dependencies (including devDependencies for build)
RUN npm ci

# Copy all source code
COPY . .

# Build the application for production
RUN npm run build

# ===================================================================
# Production stage - use nginx to serve static files
FROM nginx:alpine AS runtime

# Remove default nginx website
RUN rm /etc/nginx/conf.d/default.conf

# Copy nginx configurations
COPY nginx.dev.conf /etc/nginx/conf.d/nginx.dev.conf
COPY nginx.prod.conf /etc/nginx/conf.d/nginx.prod.conf

# Copy built React application from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy startup script that will choose the right config and make it executable
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Ensure proper permissions
RUN chown -R nginx:nginx /usr/share/nginx/html
RUN chmod -R 755 /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

# Use custom entrypoint to select config
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]