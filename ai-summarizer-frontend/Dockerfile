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

# Install ALL dependencies (including devDependencies needed for build)
# FIXED: Removed --only=production flag that was breaking the build
RUN npm ci

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

# Ensure proper permissions for nginx content
# Note: nginx:alpine runs as root by default, which is actually expected behavior
# The nginx worker processes run as nginx user, controlled by nginx.conf
RUN chmod -R 755 /usr/share/nginx/html

# Expose port 80 for the web server
EXPOSE 80

# Add health check for the nginx server
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

# Start nginx in foreground mode (required for containers)
CMD ["nginx", "-g", "daemon off;"]