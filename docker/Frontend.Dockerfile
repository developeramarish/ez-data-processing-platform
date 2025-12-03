# Frontend Dockerfile
# Multi-stage build: Node build + Nginx serve

FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY src/Frontend/package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY src/Frontend/ ./

# Build React app
RUN npm run build

# Production image
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Remove default nginx static files
RUN rm -rf ./*

# Copy built app from build stage
COPY --from=build /app/build .

# Copy nginx configuration
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
