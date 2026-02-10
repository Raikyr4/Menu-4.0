const express = require('express');
const path = require('path');
const bodyParser = require('body-parser');
const routes = require('./routes');

const app = express();
const menuPath = path.join(__dirname, '..');

app.use(express.static(menuPath));
app.use(bodyParser.json());
app.use(routes);

app.listen(3000, () => {
    console.log('Servidor rodando na porta 3000');
});
