// DigitalOcean Spaces Deployment Script
// Uploads static assets to DO Spaces with CDN

const AWS = require('aws-sdk');
const fs = require('fs');
const path = require('path');
const mime = require('mime-types');

// Configure Spaces (S3-compatible)
const spacesEndpoint = new AWS.Endpoint('nyc3.digitaloceanspaces.com');
const s3 = new AWS.S3({
  endpoint: spacesEndpoint,
  accessKeyId: process.env.DO_SPACES_ACCESS_KEY,
  secretAccessKey: process.env.DO_SPACES_SECRET_KEY,
  region: 'nyc3',
  s3ForcePathStyle: true,
  signatureVersion: 'v4'
});

const BUCKET_NAME = 'mibo-storage';
const BUILD_DIR = './dist';
const CDN_URL = 'https://mibo-storage.nyc3.cdn.digitaloceanspaces.com';

// Upload function
async function uploadFile(filePath, key) {
  const fileContent = fs.readFileSync(filePath);
  const contentType = mime.lookup(filePath) || 'application/octet-stream';

  // Determine cache control based on file type
  let cacheControl = 'public, max-age=31536000'; // 1 year for assets
  if (key.includes('index.html') || key.endsWith('.html')) {
    cacheControl = 'no-cache, no-store, must-revalidate';
  }

  const params = {
    Bucket: BUCKET_NAME,
    Key: key,
    Body: fileContent,
    ContentType: contentType,
    CacheControl: cacheControl,
    ACL: 'public-read'
  };

  try {
    const result = await s3.upload(params).promise();
    console.log(`✓ Uploaded: ${key}`);
    return result;
  } catch (error) {
    console.error(`✗ Failed to upload ${key}:`, error.message);
    throw error;
  }
}

// Recursively get all files
function getAllFiles(dirPath, arrayOfFiles = []) {
  const files = fs.readdirSync(dirPath);

  files.forEach(file => {
    const filePath = path.join(dirPath, file);
    if (fs.statSync(filePath).isDirectory()) {
      arrayOfFiles = getAllFiles(filePath, arrayOfFiles);
    } else {
      arrayOfFiles.push(filePath);
    }
  });

  return arrayOfFiles;
}

// Main deployment function
async function deploy() {
  console.log('🚀 Deploying to DigitalOcean Spaces...');
  console.log(`Bucket: ${BUCKET_NAME}`);
  console.log(`CDN URL: ${CDN_URL}`);
  console.log('');

  try {
    // Check if bucket exists
    try {
      await s3.headBucket({ Bucket: BUCKET_NAME }).promise();
      console.log('✓ Bucket exists');
    } catch (error) {
      if (error.statusCode === 404) {
        console.log('Creating bucket...');
        await s3.createBucket({ Bucket: BUCKET_NAME, ACL: 'public-read' }).promise();
        console.log('✓ Bucket created');

        // Enable CDN
        console.log('Note: Enable CDN manually in DigitalOcean console for best performance');
      }
    }

    // Get all files to upload
    const files = getAllFiles(BUILD_DIR);
    console.log(`Found ${files.length} files to upload`);

    // Upload files
    for (const file of files) {
      const key = path.relative(BUILD_DIR, file).replace(/\\/g, '/');
      await uploadFile(file, key);
    }

    // Configure CORS
    const corsConfiguration = {
      CORSRules: [
        {
          AllowedHeaders: ['*'],
          AllowedMethods: ['GET', 'HEAD'],
          AllowedOrigins: ['*'],
          MaxAgeSeconds: 3000
        }
      ]
    };

    await s3.putBucketCors({
      Bucket: BUCKET_NAME,
      CORSConfiguration: corsConfiguration
    }).promise();
    console.log('✓ CORS configured');

    // Set bucket policy for public read
    const bucketPolicy = {
      Version: '2012-10-17',
      Statement: [
        {
          Sid: 'PublicReadGetObject',
          Effect: 'Allow',
          Principal: '*',
          Action: 's3:GetObject',
          Resource: `arn:aws:s3:::${BUCKET_NAME}/*`
        }
      ]
    };

    await s3.putBucketPolicy({
      Bucket: BUCKET_NAME,
      Policy: JSON.stringify(bucketPolicy)
    }).promise();
    console.log('✓ Bucket policy configured');

    console.log('');
    console.log('✅ Deployment complete!');
    console.log(`Your app is available at: ${CDN_URL}/index.html`);

    // Output environment variables for the app
    console.log('');
    console.log('Add these environment variables to your app:');
    console.log(`VITE_CDN_URL=${CDN_URL}`);
    console.log(`VITE_SPACES_BUCKET=${BUCKET_NAME}`);

  } catch (error) {
    console.error('Deployment failed:', error);
    process.exit(1);
  }
}

// Run deployment
if (require.main === module) {
  deploy();
}

module.exports = { deploy, uploadFile };