function saveMood(mood){
    var form = $('<form action="/" method="post"><input type="text" name="location" value="' + $("#locations").val() + '" /><input type="text" name="mood" value="' + mood +'"/></form>');
    $('body').append(form);
    form.submit();
}