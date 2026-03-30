const http = require('http');
const server = http.createServer((req, res) => {
    res.statusCode = 200;
    res.setHeader('Content-Type', 'text/plain');
    res.end('TestSprite Dummy Server Running');
});
server.listen(8080, '127.0.0.1', () => {
    console.log('Dummy server running on port 8080');
});
