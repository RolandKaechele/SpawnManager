const fs = require('fs');
const path = require('path');

const streamingAssetsPath = path.join(__dirname, '..', '..', 'StreamingAssets');

if (!fs.existsSync(streamingAssetsPath)) {
    fs.mkdirSync(streamingAssetsPath, { recursive: true });
    console.log('Created StreamingAssets folder.');
} else {
    console.log('StreamingAssets folder already exists.');
}
