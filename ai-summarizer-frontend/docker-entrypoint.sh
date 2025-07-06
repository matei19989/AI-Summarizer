#!/bin/sh
# ===================================================================
# Docker entrypoint script for nginx configuration selection
# Chooses between development and production nginx configs
# ===================================================================

set -e

# Default to production if not specified
NGINX_ENV=${NGINX_ENV:-production}

echo "Starting nginx with environment: $NGINX_ENV"

# Choose the appropriate nginx configuration
if [ "$NGINX_ENV" = "development" ]; then
    echo "Using development nginx configuration..."
    cp /etc/nginx/conf.d/nginx.dev.conf /etc/nginx/conf.d/default.conf
else
    echo "Using production nginx configuration..."
    cp /etc/nginx/conf.d/nginx.prod.conf /etc/nginx/conf.d/default.conf
fi

# Remove the template files to avoid confusion
rm -f /etc/nginx/conf.d/nginx.dev.conf
rm -f /etc/nginx/conf.d/nginx.prod.conf

# Test nginx configuration
echo "Testing nginx configuration..."
nginx -t

echo "Configuration test passed. Starting nginx..."

# Execute the main command
exec "$@"