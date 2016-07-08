var d3 = Plotly.d3;

var WIDTH_IN_PERCENT_OF_PARENT = 100,
    HEIGHT_IN_PERCENT_OF_PARENT = 100;

var gd3 = d3.select('body')
    .append('div')
    .style({
        width: WIDTH_IN_PERCENT_OF_PARENT + '%',
        'margin-left': (100 - WIDTH_IN_PERCENT_OF_PARENT) / 2 + '%',

        height: HEIGHT_IN_PERCENT_OF_PARENT + 'vh',
        'margin-top': (100 - HEIGHT_IN_PERCENT_OF_PARENT) / 2 + 'vh'
    });

var gd = gd3.node();