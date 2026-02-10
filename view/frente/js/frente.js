$(function(){
    Redirecionar();
    $('.nome').text(sessionStorage.getItem('nome'));
});

function Redirecionar(){
    let mybutton = $('.botoes'); 
    mybutton.click(function(){
        window.location.href = '../' + $(this).data("tipo") + '/' + $(this).data("tipo") + '.html';
    });
}
