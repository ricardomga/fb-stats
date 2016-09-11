// facebook aplication Id
var appId = "890079291110479";

// acess token for the facebook page that is selected
var pageAccessToken;

//list of facebook pages manged by the user
var fbPages = [];

// items mapped by store
var itemsByPage = [];

var statisticsByItem = [];

window.fbAsyncInit = function () {
    FB.init({
        appId: appId,
        cookie: true,  // enable cookies to allow the server to access the session
        xfbml: true,  // parse social plugins on this page
        version: 'v2.5' // use graph api version 2.5
    });

    // Now that we've initialized the JavaScript SDK, we call
    // checkLoginState().  This function will trigger the FB.getLoginStatus that 
    //gets the state of the business suite user visiting this page and can return 
    //one of three states to the callback you provide.  They can be:
    //
    // 1. Logged into your app ('connected')
    // 2. Logged into Facebook, but not your app ('not_authorized')
    // 3. Not logged into Facebook and can't tell if they are logged into
    //    your app or not.
    //
    // These three cases are handled in the callback function.

    checkLoginState();

};


// This function is called when someone finishes with the Login
// Button.  See the onlogin handler attached to it in the sample
// code below.
function checkLoginState() {
    FB.getLoginStatus(function (response) {
        statusChangeCallback(response);
    });
}


// This is called with the results from from FB.getLoginStatus().
function statusChangeCallback(response) {
    // The response object is returned with a status field that lets the
    // app know the current login status of the business suite user.
    if (response.status === 'connected') {
        // Logged into your app and Facebook.

        console.log(response);
        logginMessage();
        getFbPagesManagedByUser();
    } else if (response.status === 'not_authorized') {
        // The busineess suite user is logged into Facebook, but not your app.
        $('#status').text('Necessita de aceitar os termos da aplicação!');
    } else {
        // The busineess suite user is not logged into Facebook, so we're not sure if
        // they are logged into this app or not.
        $('#status').text('Necessita de entrar no facebook!');
    }
}


//this function shows to the user a message indication wich facebook account he is logged in
function logginMessage() {
    FB.api('/me',
        function (response) {
            $('#status').text('Está logado com a conta, ' + response.name + '!');
        });
}


//this function will get the facebook pages that the user manages 
//and put them in a drop down list, so the user choose the page he
//wants to add the Facebook tab
function getFbPagesManagedByUser() {
    FB.api("me/?fields=accounts", loadFbPages);
}


function loadFbPages(response) {
    if (response && !response.error) {
        //Checks if the facebook user manages any page, if he doens't manges 
        //any page is showed a message to the user and the function stops 
        if (response.accounts.data.length == 0) {
            //  messageAlert("Necessita de gerir pelo menos uma página!", "info");
            console.log("Necessita de gerir pelo menos uma página!");
            return;
        }

        //put the retrived fb pages in a the global variavel fbPages
        fbPages = response.accounts.data;

        var select = $("#page");
        for (var i = 0; i < fbPages.length; i++) {
            //create a new option for each page and puts the fb page id in the option value
            select.append($("<option></option>").val(fbPages[i].id).text(fbPages[i].name));
        }
        setAccessToken();
      //  getBSItems();
    } else {
        //error message if the response is not the expected
        // messageAlert("Erro ao acessar as páginas que gere!", "danger");
        console.log("Erro ao acessar as páginas que gere!");
    }
}


//this function set the access token of the current selected page in the drop down list
function setAccessToken() {
    //gets the selected fb page id and put it in the global variavel pageId
    fbStoreId = $("#page").val();
    //iterates the global variavel fbPages array
    for (var i = 0; i < fbPages.length; i++) {
        //when the ids match it takes the fb page access token and put it 
        //in the global variavel pageAccessToken for future fb requests
        if (fbStoreId == fbPages[i].id) {
            $("#accessToken").val(fbPages[i].access_token);
            break;
        }
    }
}

/*
function getBSItems() {
    $("#item").empty();
    setAccessToken();

    if (itemsByPage[fbStoreId]) {
        loadItems(itemsByPage[fbStoreId]);
    } else {
        $.get("https://socialbs.azurewebsites.net/api/items?fbStoreId=" + fbStoreId, onGetItemsSuccess);
    }
}


function onGetItemsSuccess(items) {
    sortItemsByCategory(items);
    loadItems(items);
    itemsByPage[fbStoreId] = items;
}


function sortItemsByCategory(items) {
    items.sort(function (a, b) {
        if (a.category > b.category) {
            return 1;
        } else if (a.category < b.category) {
            return -1;
        }
        return 0;
    });
}


function loadItems(items) {
    var categories = [];
    items.forEach(function (item) {
        if (!categories.includes(item.category)) {
            categories.push(item.category);
            $("#item").append($("<option></option>").val("category").html(" -> " + item.category + " <- "));
        }
        $("#item").append($("<option></option>").val(item.ItemKey).text(item.Description));

        getItemStatistics(item.ItemKey);
    });

}

function getItemStatistics(itemId) {
    getUrlId(itemId)
}


function getUrlId(itemId) {
    console.log("getting url id");

    FB.api("/",
     {
         access_token: pageAccessToken,
         id: "https://socialbs.azurewebsites.net/" + fbStoreId + "/products/" + itemId,
         fields: "og_object{id}, share"
     },
     fbUrlIdRes);

}


function fbUrlIdRes(response) {
    console.log("reading fb url resp", response);
    if (response && !response.error) {
        var splitedId = response.id.split("/");
        var itemId = splitedId[splitedId.length - 1];
        getStatisticsById(response.og_object.id, itemId);
        statisticsByItem[itemId] = {};
        statisticsByItem[itemId].sharesCount = response.share.share_count;
    }
}


function getStatisticsById(ogId, itemId) {
    FB.api("/" + ogId,
        {
            access_token: pageAccessToken,
            fields: "likes, comments, engagement"
        },
        function (response) { fbStatisticsRes(response, itemId) });
}


function fbStatisticsRes(response, itemId) {
    if (response && !response.error) {

        if (response.likes) {
            statisticsByItem[itemId].likes = response.likes.data;
        }
        if (response.comments) {
            statisticsByItem[itemId].comments = response.comments.data;
        }
        console.log(statisticsByItem[itemId]);


    } else {
        console.log(response);
    }
}


function showStatistics(itemStatistics) {
    $("#comments").empty();
    $("#likes").empty();

    $("#statisticsHeader").text("Estatisticas do produto " + $("#item option:selected").text());
    $("#shares_count").text("Partilhas: " + itemStatistics.sharesCount);

    if (itemStatistics.comments) {
        $("#commentsCount").text(itemStatistics.comments.length + " Comentários");
        itemStatistics.comments.forEach(function (comment) {
            $("#comments").append("<li>" + comment.created_time + "</li>")
            $("#comments").append('<li><a href="https://facebook.com/' + comment.from.id + '" target="_blank">' + comment.from.name + '</a></li>');
            $("#comments").append("<ul><li>" + comment.message + "<br/><br/></li></ul>");
        });
    } else {
        $("#commentsCount").text("Ninguém comentou o produto selecionado.");
    }

    if (itemStatistics.likes) {
        $("#likesCount").text(itemStatistics.likes.length + " Gostos");
        itemStatistics.likes.forEach(function (like) {
            $("#likes").append('<li><a href="https://facebook.com/' + like.id + '" target="_blank">' + like.name + '</a><br/></li>');
        });
    } else {
        $("#likesCount").text("Ninguém gostou do produto selecionado.");
    }
}
 */


// Load the facebook SDK asynchronously
(function (d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s); js.id = id;
    js.src = "//connect.facebook.net/pt_PT/sdk.js";
    fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));


$(document).ready(function () {
   // $("#page").change(getBSItems);
    $(".fb-login-button").click(checkLoginState);
    $("#page").change(setAccessToken);
    //$("#item").change(function () {
    //    var selected = $("#item").val();
    //    if (selected !== "category") {
    //        showStatistics(statisticsByItem[selected]);
    //    }
    //});

});
