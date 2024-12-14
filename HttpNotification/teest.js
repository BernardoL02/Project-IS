const express = require('express');
const bodyParser = require('body-parser');
require('body-parser-xml')(bodyParser);

const app = express();
const port = 8080;

// Middleware para lidar com XML
app.use(bodyParser.xml({
    limit: '1MB', // Tamanho máximo do XML
    xmlParseOptions: {
        normalize: true,     // Normaliza espaços em branco
        normalizeTags: true, // Converte tags para letras minúsculas
        explicitArray: false // Força arrays apenas quando necessário
    }
}));

app.post('/Channel2', (req, res) => {
    console.log('Recebido POST:', req.body); 
    res.status(200).send('<response><status>success</status></response>');
});


app.listen(port, () => {
    console.log(`Servidor rodando em http://127.0.0.1:${port}`);
});
