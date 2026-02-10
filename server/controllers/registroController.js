const pool = require('../db');

exports.postRegistro = async (req, res) => {
    
    const {nomeVariavel, total, pagamentos, produtos} = req.body;

    if (nomeVariavel == 'balcao_id') {

        const query = 'INSERT INTO tb_registro_diario (atendimento_tipo, total, pagamentos, produtos) VALUES($1, $2, $3, $4)';
        const result = await pool.query(query, [nomeVariavel, total, pagamentos, produtos]);
        res.status(200).json(result.rows);
        
    } else if (nomeVariavel == 'mesa_id') {

        const query = 'INSERT INTO tb_registro_diario (atendimento_tipo, total, pagamentos, produtos) VALUES($1, $2, $3, $4)';
        const result = await pool.query(query, [nomeVariavel, total, pagamentos, produtos]);
        res.status(200).json(result.rows);
    }
};

exports.postPedidoBalcao = async (req, res) => {

    const query = 'INSERT INTO tb_balcao (horario) VALUES (CURRENT_TIME) RETURNING id, horario';
    const result = await pool.query(query);
    res.status(200).json(result.rows);
}

exports.postExcluiPedidoBalcao = async (req, res) => {

    const {id} = req.body;

    const query = 'DELETE FROM tb_balcao WHERE id = $1';
    const result = await pool.query(query , [id]);
    res.status(200).json(result.rows);
}


