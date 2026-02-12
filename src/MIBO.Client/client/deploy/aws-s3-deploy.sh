#!/bin/bash

# AWS S3 Deployment Script for React App
# Usage: ./aws-s3-deploy.sh <environment>

set -e

# Configuration
ENVIRONMENT=${1:-development}
BUILD_DIR="./dist"
CLOUDFRONT_ENABLED=true

# Environment-specific settings
case $ENVIRONMENT in
  "production")
    S3_BUCKET="mibo-client-prod"
    CLOUDFRONT_ID="E1234567890ABC"
    API_URL="https://api.mibo.com"
    ;;
  "staging")
    S3_BUCKET="mibo-client-staging"
    CLOUDFRONT_ID="E0987654321XYZ"
    API_URL="https://staging-api.mibo.com"
    ;;
  "development")
    S3_BUCKET="mibo-client-dev"
    CLOUDFRONT_ID="E1111111111AAA"
    API_URL="https://dev-api.mibo.com"
    ;;
  *)
    echo "Invalid environment: $ENVIRONMENT"
    echo "Usage: $0 [development|staging|production]"
    exit 1
    ;;
esac

echo "Deploying to $ENVIRONMENT environment..."
echo "S3 Bucket: $S3_BUCKET"

# Build the application
echo "Building application..."
VITE_API_URL=$API_URL VITE_ENV=$ENVIRONMENT npm run build

# Sync to S3
echo "Uploading to S3..."
aws s3 sync $BUILD_DIR s3://$S3_BUCKET \
  --delete \
  --cache-control max-age=31536000 \
  --exclude "index.html" \
  --exclude "*.map"

# Upload index.html with no-cache
aws s3 cp $BUILD_DIR/index.html s3://$S3_BUCKET/index.html \
  --cache-control "no-cache, no-store, must-revalidate" \
  --content-type "text/html"

# Invalidate CloudFront cache
if [ "$CLOUDFRONT_ENABLED" = true ] && [ -n "$CLOUDFRONT_ID" ]; then
  echo "Creating CloudFront invalidation..."
  aws cloudfront create-invalidation \
    --distribution-id $CLOUDFRONT_ID \
    --paths "/*"
fi

echo "Deployment to $ENVIRONMENT completed successfully!"