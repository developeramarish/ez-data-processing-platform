# MkDocs Documentation Site for EZ Platform v0.1.0-beta
# Serves all documentation as static HTML site

FROM python:3.11-slim

WORKDIR /docs

# Install MkDocs and Material theme
RUN pip install --no-cache-dir \
    mkdocs==1.5.3 \
    mkdocs-material==9.5.3

# Copy documentation source
COPY mkdocs.yml .
COPY docs/ docs/

# Build static site
RUN mkdocs build

# Use nginx to serve static files
FROM nginx:alpine
COPY --from=0 /docs/site /usr/share/nginx/html

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
  CMD wget -q --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
