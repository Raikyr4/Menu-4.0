const pool = require('../db');


exports.postComanda = async (req, res) => {
    
    const { idString, total, total_taxa, taxa, restante, nomeVariavel, valorVariavel } = req.body;

        if (nomeVariavel == 'balcao_id') {

            const query = 'UPDATE tb_balcao SET produtos = $1, total = $2, restante = $3 WHERE id = $4 RETURNING id';
            await pool.query(query, [idString, total, restante, valorVariavel]);
            res.status(200).json({ message: 'Comanda Salva com sucesso' });
            
        } else if (nomeVariavel == 'mesa_id') {

            const query = 'UPDATE tb_mesa SET produtos = $1, total = $2, total_taxa = $3, taxa = $5, restante = $4 WHERE id = $6 RETURNING id';
            await pool.query(query, [idString, total, total_taxa, restante, taxa, valorVariavel]);
            res.status(200).json({ message: 'Comanda Salva com sucesso' });

        } else{
            res.status(500).json({ message: 'Ocorreu um erro ao salvar a comanda' });
        }

};



exports.getComanda = async (req, res) => {
    
    const {nomeVariavel, valorVariavel} = req.body;

    if (nomeVariavel == 'balcao_id') {

        const query = 'SELECT * FROM tb_balcao WHERE id = $1';
        const result = await pool.query(query, [valorVariavel]);
        res.status(200).json(result.rows);
        
    } else if (nomeVariavel == 'mesa_id') {

        const query = 'SELECT * FROM tb_mesa WHERE id = $1';
        const result = await pool.query(query, [valorVariavel]);
        res.status(200).json(result.rows);

    }
};


exports.pagamento = async (req, res) => {
    
    const {nomeVariavel, valorVariavel, soma, stringPagamento, restante} = req.body;

    if (nomeVariavel == 'balcao_id') {

        const query = 'UPDATE tb_balcao SET pago = $2, pagamentos = $3, restante = $4 WHERE id = $1';
        const result = await pool.query(query, [valorVariavel, soma, stringPagamento, restante]);
        res.status(200).json(result.rows);
        
    } else if (nomeVariavel == 'mesa_id') {

        const query = 'UPDATE tb_mesa SET pago = $2, pagamentos = $3, restante = $4  WHERE id = $1';
        const result = await pool.query(query, [valorVariavel, soma, stringPagamento, restante]);
        res.status(200).json(result.rows);

    }
};

exports.EncerraComanda = async (req, res) => {
    
    const {nomeVariavel, valorVariavel} = req.body;

    if (nomeVariavel == 'balcao_id') {

        const query = "UPDATE tb_balcao SET fechado = true WHERE id = $1 ";
        const result = await pool.query(query, [valorVariavel]);
        res.status(200).json(result.rows);
        
    } else if (nomeVariavel == 'mesa_id') {

        const query = "UPDATE tb_mesa SET total = 0, total_taxa = 0, taxa = 0, pago = 0, restante = 0, produtos = '', pagamentos = '' WHERE id = $1 ";
        const result = await pool.query(query, [valorVariavel]);
        res.status(200).json(result.rows);

    }
};


exports.getMesasOcupadas = async (req, res) => {
    
    const query = 'SELECT * FROM tb_mesa';
    const result = await pool.query(query);
    res.status(200).json(result.rows);
}

exports.getPedidosBalcoes = async (req, res) => {
    
    const query = 'SELECT * FROM tb_balcao ORDER BY id ASC';
    const result = await pool.query(query);
    res.status(200).json(result.rows);
}