function setAgo(subject) {
    var ago = moment().subtract(1, subject);
    var now = moment();

    document.getElementById('year_4').value = now.format('YYYY');
    document.getElementById('month_4').value = now.format('MM');
    document.getElementById('day_4').value = now.format('DD');
    document.getElementById('year_1').value = ago.format('YYYY');
    document.getElementById('month_1').value = ago.format('MM');
    document.getElementById('day_1').value = ago.format('DD');
}