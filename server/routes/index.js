const express = require('express');
const router = express.Router();
const { login, register, verTotal, verMesa, getCategorias, getProdutos, postComanda, getComanda, pagamento, postRegistro, EncerraComanda, getMesasOcupadas, getPedidosBalcoes, postPedidoBalcao , postExcluiPedidoBalcao} = require('../controllers');

router.post('/login', login);
router.post('/register', register);
router.get('/verTotal', verTotal);
router.get('/verMesa', verMesa);
router.get('/categorias', getCategorias);
router.get('/produtos', getProdutos);
router.post('/postComanda', postComanda);
router.post('/getComanda', getComanda);
router.post('/pagamento', pagamento);
router.post('/postRegistro', postRegistro);
router.post('/EncerraComanda', EncerraComanda);
router.get('/getMesasOcupadas', getMesasOcupadas);
router.get('/getPedidosBalcoes', getPedidosBalcoes);
router.post('/postPedidoBalcao', postPedidoBalcao);
router.post('/postExcluiPedidoBalcao', postExcluiPedidoBalcao);


module.exports = router;
