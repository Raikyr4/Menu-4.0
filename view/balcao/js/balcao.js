$(function () {
    $('.nome').text(sessionStorage.getItem('nome'));
    // $('.myButtons').css('width', '90%');
    CarregaLista();
    VerMontante();
    NovoPedido();
    Redirecionar();
    ExcluirPedido();
    VerComanda();
})


function Redirecionar() {
    let back = $('.back');
    back.click(function () {
        window.location.href = '../frente/frente.html';
    });
}

function NovoPedido(){

    let mybutton = $('.novoPedido');
    mybutton.on('click',function () {

        $.ajax({
            url: '/postPedidoBalcao',
            method: 'POST',
            success: function (response) {

                window.location.href = '../comanda/comanda.html?balcao_id=' + response[0].id;
            },
            error: function (xhr, status, error) {
                // Mostrar erro
                console.log('Erro ao gerar novo pedido no balcao.');
            }
        });

    });
}


function CalculaMontante() {
    $.ajax({
        url: '/verTotal',
        method: 'GET',
        success: function (response) {

            var somaTotal = response.somaTotal;

            // Formatar como moeda
            var somaTotalFormatada = 'R$ ' + parseFloat(somaTotal).toLocaleString('pt-BR', { minimumFractionDigits: 2 });

            $(".montante").text(somaTotalFormatada);
        },
        error: function (xhr, status, error) {
            // Mostrar erro
            console.log('Erro ao buscar a soma total: ' + xhr.responseText);
        }
    });
}

function VerMontante() {
    let eye = $(".see");
    eye.click(function () {
        if ($(this).text() == "visibility_off") {

            $(this).text("visibility");
            CalculaMontante();
        }
        else {
            $(this).text("visibility_off");
            $(".montante").text("R$ ****,**");
        }
    });
}

function CarregaLista() {

    $.ajax({
        url: '/getPedidosBalcoes',
        method: 'get',
        success: function (response) {
            let fechado;
            let marc;
            for (let i = 0; i < response.length; i++) {
                
                if(!response[i].fechado){
                    fechado = 'visibility';
                    marc = 'ver';
                }
                else{
                    fechado = 'description';
                    marc = 'imprimir';
                }

                let newItem = '<li><div>Pedido ' + response[i].id + '<div href="#!" class="secondary-content">' + response[i].horario.substring(0, 5) + ' - <i class="material-icons ' + marc + '" data-id="' + response[i].id + '">' + fechado + '</i><i class="material-icons excluir" data-id="'+ response[i].id +'">delete</i></div></div></li>';
                $('.collection.lista').prepend(newItem);
            }
        },
        error: function (xhr, status, error) {
            // Mostrar erro
            console.log('Erro ao carregar lista de pedidos.');
        }
    });
}

function ExcluirPedido()
{
    $('.collection.lista').on('click', '.excluir', function(){

        let listItem = $(this).closest('li'); // Find the closest <li> ancestor

        $.ajax({
            url: '/postExcluiPedidoBalcao',
            method: 'POST',
            data: JSON.stringify({
                id: $(this).data('id')
            }),
            contentType: 'application/json',
            success: function (response) {
                listItem.remove();
            },
            error: function (xhr, status, error) {
                // Mostrar erro
                console.log('Erro ao excluir pedido.');
            }
        });
    });
}

function VerComanda()
{
    $('.collection.lista').on('click', '.ver', function(){

        window.location.href = '../comanda/comanda.html?balcao_id=' + $(this).data('id');
    });
}


function ImprimirPedido()
{

}
