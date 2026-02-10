const { Pool } = require('pg');

const pool = new Pool({
    host: 'localhost', 
    user: 'postgres', 
    password: '110903', 
    database: 'postgres', 
    port: 5432, 
});

module.exports = pool;
