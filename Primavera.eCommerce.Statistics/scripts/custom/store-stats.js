$(document).ready(function () {
    alert("store-stats ready");
    var fbStoreId = $("#fbStoreId").val();
    var url = "/statistics/GetGeneralStats?fbStoreId=" + fbStoreId;

    var table = $("#table").DataTable({
        "ajax":{
            "url": url.trim(),
            "type": "POST",
            "datatype": "json"   
        },
        "columns": [
            { "data": "ItemKey", "auto-with": true },
            { "data": "ItemDescription", "auto-with": true },
            { "data": "NumLikes", "auto-with": true },
            { "data": "NumComments", "auto-with": true },
            { "data": "NumShares", "auto-with": true }
        ]
    });

    $("#table tbody").on("click", "tr", onTableRowClick);

    function onTableRowClick() {
        var itemKey = table.row(this).data().ItemKey;
        selectRow();
        getItemStatsDetails(itemKey);
    }

    function selectRow() {
        if ($(this).hasClass("selected")) {
            $(this).removeClass("selected");
        }
        else {
            table.$("tr.selected").removeClass("selected");
            $(this).addClass("selected");
        }
    }

    function getItemStatsDetails(itemKey) {
        $.ajax({
            url: "/statistics/itemdata",
            method: "POST",
            data: { fbStoreId: fbStoreId, itemKey: itemKey },
            dataType: "json",
            success: showItemDetails
        });
    }

    function showItemDetails(itemStatistics) {
        console.log(itemStatistics);
        removePrevItemDetails();

        $("#statisticsHeader").text("Estatisticas do produto " + itemStatistics.Description);
        $("#shares_count").text("Partilhas: " + itemStatistics.ShareCount);

        showCommentsData(itemStatistics.Comments);

        showLikesData(itemStatistics.Likes);
    }

    function removePrevItemDetails() {
        $("#comments").empty();
        $("#likes").empty();
    }

    function showCommentsData(comments) {
        if (comments) {
            $("#commentsCount").text(comments.length + " Comentários");
            comments.forEach(function (comment) {
                $("#comments").append("<li>" + comment.CreatedTime + "</li>");
                $("#comments").append('<li><a href="https://facebook.com/' + comment.FbUserId + '" target="_blank">' + comment.FbUser.Name + '</a></li>');
                $("#comments").append("<ul><li>" + comment.Message + "<br/><br/></li></ul>");
            });
        } else {
            $("#commentsCount").text("Ninguém comentou o produto selecionado.");
        }
    }

    function showLikesData(likes) {
        if (likes) {
            $("#likesCount").text(likes.length + " Gostos");
            likes.forEach(function (like) {
                $("#likes").append('<li><a href="https://facebook.com/' + like.FbUserId + '" target="_blank">' + like.FbUser.Name + '</a><br/></li>');
            });
        } else {
            $("#likesCount").text("Ninguém gostou do produto selecionado.");
        }
    }
});

