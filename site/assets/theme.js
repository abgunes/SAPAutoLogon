(function () {
  try {
    var saved = localStorage.getItem('sapautologon-mode');
    if (saved === 'light' || saved === 'dark') { document.documentElement.setAttribute('data-mode', saved); }
  } catch (e) { /* storage blocked: follow the system setting */ }
})();
