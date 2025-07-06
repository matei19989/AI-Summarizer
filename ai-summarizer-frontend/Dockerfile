# ===================================================================
# Multi-stage Dockerfile for React + Vite Frontend
# Stage 1: Build the React application
# Stage 2: Serve with nginx for production
# ===================================================================

# Build stage - use Node.js to build the React application
FROM node:18-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files first for better layer caching
# This allows npm install to be cached if dependencies don't change
COPY package*.json ./

# Install dependencies
# Using npm ci for faster, reliable, reproducible builds
RUN npm ci --only=production

# Copy all source code
COPY . .

# Build the application for production
# Vite will create optimized bundle in dist/ directory
RUN npm run build

# ===================================================================
# Production stage - use nginx to serve the static files
FROM nginx:alpine AS runtime

# Copy custom nginx configuration
# We'll create this to handle SPA routing and API proxying
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built React application from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Create non-root user for security
RUN addgroup -g 101 -S nginx && \
    adduser -S -D -H -u 101 -h /var/cache/nginx -s /sbin/nologin -G nginx -g nginx nginx

# Set proper permissions for nginx directories
RUN chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d && \
    touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid && \
    chown -R nginx:nginx /usr/share/nginx/html

# Switch to non-root user
USER nginx

# Expose port 80 for the web server
EXPOSE 80

# Add health check for the nginx server
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

# Start nginx in foreground mode (required for containers)
CMD ["nginx", "-g", "daemon off;"]