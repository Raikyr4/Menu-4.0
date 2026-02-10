/*

    A função a baixo (Login()) serve para realizar o login do usuário.
    
    Ela realiza  uma requisição AJAX para um ponto do meu servidor que
    verifica se o usuário existe e se a senha ou o username que foi digitado 
    está correto. Se der tudo certo na verificação ele é redirecionado para a
    frente de loja, onde ele poderá acessar o sistema.

*/
function Login() {
    
    var username = $('#username_L').val();
    var password = $('#password_L').val();

    $.ajax({
        url: '/login',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ username: username, password: password }),
        success: function(response) {

            sessionStorage.setItem('nome', response.nome);

            // Login bem sucedido redireciona para outra pagina 
            window.location.href = '/view/frente/frente.html';

            history.pushState(null, null, location.href);
            window.onpopstate = function () {
                history.go(1);
            };
        },
        error: function(xhr, status, error) {
            // Mostrar erro
            alert(xhr.responseText);
        }
    });

}


/*
    A função abaixo (Cadastrar()) serve para realizar o cadastro de um novo usuário no sistema.

    Ela realiza uma validação de senha do usuário para que ele tenha os requisitos de segurança 
    Além disso ela realiza uma requisição AJAX que acessa um ponto no nosso servidor que 
    insere os dados do usuário em nosso sistema. Se algo der errado ele retorna uma mensagem informando 
    que algo deu errado com a comunicação com o banco de dados ou até mesmo com server.js.
 */
function Cadastrar() {
    let nome = $('#nome_C').val();
    let username = $('#username_C').val();
    let password = $('#password_C').val();

    // let regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
    // if (!regex.test(password)) {
    //     alert('A senha deve conter pelo menos um caractere especial, um número e uma letra maiúscula.');
    //     return;
    // }

    $.ajax({
        type: 'POST',
        url: '/register',
        data: JSON.stringify({
            nome: nome,
            username: username,
            password: password
        }),
        contentType: 'application/json',
        success: function(response) {
            alert('Usuário criado com sucesso!');
        },
        error: function(xhr) {
            alert('Request failed.  Returned status of ' + xhr.status);
        }
    });
}
