const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

console.log('=== BUILD TEST ===');
console.log('CWD:', process.cwd());
console.log('');

// Check files exist
console.log('index.html exists:', fs.existsSync('index.html'));
console.log('vite.config.js exists:', fs.existsSync('vite.config.js'));
console.log('src/main.js exists:', fs.existsSync('src/main.js'));
console.log('');

// Check node_modules
console.log('node_modules exists:', fs.existsSync('node_modules'));
console.log('vite installed:', fs.existsSync('node_modules/vite'));
console.log('');

// Run build
console.log('Running vite build...');
try {
    const result = execSync('npx vite build --logLevel info', { 
        encoding: 'utf8', 
        stdio: ['pipe', 'pipe', 'pipe']
    });
    console.log('Build output:');
    console.log(result);
} catch (err) {
    console.error('Build error:', err.message);
    if (err.stdout) console.log('stdout:', err.stdout);
    if (err.stderr) console.log('stderr:', err.stderr);
}

console.log('');
console.log('=== AFTER BUILD ===');
console.log('dist exists:', fs.existsSync('dist'));

if (fs.existsSync('dist')) {
    const files = fs.readdirSync('dist', { recursive: true });
    console.log('dist contents:', files);
} else {
    console.log('Checking parent for dist...');
    console.log('Parent dirs:', fs.readdirSync('..').filter(f => f.includes('dist') || f === 'wwwroot'));
}
