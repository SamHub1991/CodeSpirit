// Home Page Scripts

document.addEventListener('DOMContentLoaded', function () {
  var cards = document.querySelectorAll('.dashboard .card');
  cards.forEach(function (card) {
    card.addEventListener('click', function () {
      var link = card.querySelector('a');
      if (link) {
        window.location.href = link.getAttribute('href');
      }
    });
    card.style.cursor = 'pointer';
  });
});