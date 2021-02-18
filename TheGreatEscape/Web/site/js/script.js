
	$(window).load(function() {
	$('#slider').nivoSlider({
        effect:'fade', //Specify sets like: 'fold,fade,sliceDown, sliceDownLeft, sliceUp, sliceUpLeft, sliceUpDown, sliceUpDownLeft'    

        slices:	20,
        animSpeed:400,
        pauseTime:6000,
        startSlide:0, //Set starting Slide (0 index)
        directionNav:true, //Next & Prev
        directionNavHide:false, //Only show on hover
        controlNav:false, //1,2,3...
        controlNavThumbs:false, //Use thumbnails for Control Nav
        controlNavThumbsFromRel:false, //Use image rel for thumbs
        controlNavThumbsSearch: '.jpg', //Replace this with...
        controlNavThumbsReplace: '_thumb.jpg', //...this in thumb Image src
        keyboardNav:true, //Use left & right arrows
        pauseOnHover:true, //Stop animation while hovering
        manualAdvance:false, //Force manual transitions
        captionOpacity:1, //Universal caption opacity
        beforeChange: function(){},
        afterChange: function(){ Cufon.refresh();$('.nivo-caption').animate({right:'76'},400)},
        slideshowEnd: function(){} //Triggers after all slides have been shown
    });
	Cufon.refresh();
    });
$(document).ready(function() {
   SetData();
   function SetData() {
	    var now = new Date();
	    $('.date').html(now.getDate()+'.');
		mounth=now.getMonth()+1;
		if (mounth<10) {mounth='0'+mounth}
	    $('.date').append(mounth+'.');
		$('.date').append(now.getFullYear()+' / ');
		hour=now.getHours();
	    minutes=now.getMinutes();
	    if (minutes<10) {minutes='0'+minutes};
	    if (hour<=12) {$('.date').append(hour+'.'+minutes+' a.m.');}  else{$('.date').append(hour-12+'.'+minutes+' p.m.');}
	}
  	setInterval(SetData,60); 
});