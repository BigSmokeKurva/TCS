
function toggleOptionsMenu() {
    var $optionSelectedOption = $('#b_option_select> .selected-option');
    var $optionOptions = $('#b_option_select > .options');
    $optionOptions.is(':visible') ? $optionOptions.fadeOut(100) : $optionOptions.fadeIn(100);
    $optionSelectedOption.toggleClass('selected-option-active');
    $optionSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

function toggleEditBindsMenu() {
    var $optionSelectedOption = $('#b_edit_button_select > div');
    var $optionOptions = $('#b_edit_button_select > ul');
    $optionOptions.is(':visible') ? $optionOptions.fadeOut(100) : $optionOptions.fadeIn(100);
    $optionSelectedOption.toggleClass('selected-option-active');
    $optionSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

function toggleDeleteBindsMenu() {
    var $optionSelectedOption = $('#b_delete_button_select > div');
    var $optionOptions = $('#b_delete_button_select > ul');
    $optionOptions.is(':visible') ? $optionOptions.fadeOut(100) : $optionOptions.fadeIn(100);
    $optionSelectedOption.toggleClass('selected-option-active');
    $optionSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

function addNewBind() {
    var $bindName = $('#b_add > div > input:nth-child(2)').val();
    var $bindValue = $('#b_add > div > input:nth-child(4)').val();
    var array = $bindValue.split(',').map(function (item) {
        return item.trim();
    });
    var data = {
        name: $bindName,
        messages: array
    };
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    fetch('api/app/addBind', {
        method: 'POST',
        headers: {
            "Authorization": auth_token,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            if (data.status === "ok") {
                var btn = $('<button>', {
                    class: 'b_item',
                    text: data.bindName
                });
                btn.on('click', sendMessage);
                $('.b_binds').append(btn);
                $('#b_add > div > input:nth-child(2)').val('');
                $('#b_add > div > input:nth-child(4)').val('');
                return;
            }
            showNotification(data.message);
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}

function setupEditBinds() {
    var array = [];

    $(".b_binds-container > div > button").each(function () {
        array.push($(this).text());
    });
    var $ul = $('#b_edit_button_select > ul');
    $ul.empty();
    array.forEach(function (text) {
        var $li = $("<li></li>");
        $li.on('click', function () {
            if ($('#b_edit_button_select > ul').is(':visible')) {
                toggleEditBindsMenu();
            }
            var btn = $(this);
            var parent = btn.parent();
            parent.find('li.selected').removeClass('selected');
            btn.addClass('selected');
            var bindName = btn.text();
            $('#b_edit_button_select > div > span').text(bindName);
            $('#b_edit > div:nth-child(1) > input').val(bindName);
            const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

            fetch('api/app/getBindMessages?bindname=' + bindName, {
                method: 'GET',
                headers: {
                    "Authorization": auth_token,
                },
            })
                .then(response => {
                    if (!response.ok) {
                        throw Error();
                    }
                    return response.json();
                })
                .then(data => {
                    if (data.status === "ok") {
                        $('#b_edit > div:nth-child(2) > input').val(data.messages.join(", "));
                        return;
                    }
                    showNotification(data.message);
                })
                .catch(error => {
                    showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
                });
        });
        $li.text(text);
        $ul.append($li);
    });
    $ul.find('li').first().click();
}

function setupDeleteBinds() {
    var array = [];

    $(".b_binds-container > div > button").each(function () {
        array.push($(this).text());
    });
    var $ul = $('#b_delete_button_select > ul');
    $ul.empty();
    array.forEach(function (text) {
        var $li = $("<li></li>");
        $li.on('click', function () {
            if ($('#b_delete_button_select > ul').is(':visible')) {
                toggleDeleteBindsMenu();
            }
            var btn = $(this);
            var parent = btn.parent();
            parent.find('li.selected').removeClass('selected');
            btn.addClass('selected');
            var bindName = btn.text();
            $('#b_delete_button_select > div > span').text(bindName);
        });
        $li.text(text);
        $ul.append($li);
    });
    $ul.find('li').first().click();
}

function setOption(btn) {
    var parent = btn.parent();
    $('.b_options_content > div').hide();
    switch (btn.attr('id')) {
        case 'b_add-btn':
            $('#b_add').show();
            break;
        case 'b_edit-btn':
            setupEditBinds();
            $('#b_edit').show();
            break;
        case 'b_delete-btn':
            setupDeleteBinds();
            $('#b_delete').show();
            break;
    }
    parent.find('li.selected').removeClass('selected');
    btn.addClass('selected');
    $('#b_option_select > div > span').text(btn.text());
}

function sendMessage() {
    if (isRandom) {
        nextBtn();
    }
    var bot = $('#bots-list #bots [class="item selected-item"]');
    if (bot.length === 0) {
        return;
    }
    var botname = bot.attr('botname');
    var bindname = $(this).text()
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    fetch('api/app/sendbindmessage', {
        method: 'POST',
        headers: {
            "Authorization": auth_token,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            botname: botname,
            bindname: bindname
        })
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            if (data.status === 'ok'){
                return;
            }
            showNotification(data.message);
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });


}

$(document).ready(function () {
    $('#b_option_select > .selected-option').on('click', function (e) {
        e.stopPropagation();
        toggleOptionsMenu();
    });

    $('#b_edit_button_select > div').on('click', function (e) {
        e.stopPropagation();
        toggleEditBindsMenu();
    });

    $('#b_delete_button_select > div').on('click', function (e) {
        e.stopPropagation();
        toggleDeleteBindsMenu();
    });

    $('#b_add-btn').on('click', function () {
        toggleOptionsMenu();
        setOption($(this));
    });
    setOption($('#b_add-btn'));
    $('#b_edit-btn').on('click', function () {
        toggleOptionsMenu($(this));
        setOption($(this));
    });

    $('#b_delete-btn').on('click', function () {
        toggleOptionsMenu($(this));
        setOption($(this));
    });

    $('#b_add > div > button').on('click', function () {
        addNewBind();
    });
    $('#b_add > div > input:nth-child(4)').on('keypress', function (e) {
        if (e.which === 13) {
            addNewBind();
        }
    });

    $('#b_edit > div:nth-child(2) > button').on('click', function () {
        var $bindName = $('#b_edit > div:nth-child(1) > input').val();
        var $bindValue = $('#b_edit > div:nth-child(2) > input').val();
        var array = $bindValue.split(',').map(function (item) {
            return item.trim();
        });
        var data = {
            name: $bindName,
            oldName: $('#b_edit_button_select > div > span').text(),
            messages: array
        };
        const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
        fetch('api/app/editBind', {
            method: 'POST',
            headers: {
                "Authorization": auth_token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        })
            .then(response => {
                if (!response.ok) {
                    throw Error();
                }
                return response.json();
            })
            .then(data => {
                if (data.status === "ok") {
                    var oldName = $('#b_edit_button_select > div > span').text();
                    $('#b_edit > div:nth-child(1) > input').val(data.name);
                    $('#b_edit > div:nth-child(2) > input').val(data.messages.join(", "));
                    $('#b_edit li:contains("' + oldName + '")').text(data.name);
                    $('#b_edit_button_select > div > span').text(data.name);
                    $('.b_binds button:contains("' + oldName + '")').text(data.name);
                    return;
                }
                showNotification(data.message);
            })
            .catch(error => {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            });
    });
    $('#b_delete > div > button').on('click', function () {
        const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
        var bindName = $('#b_delete_button_select > div > span').text();
        fetch('api/app/deleteBind?bindname=' + bindName, {
            method: 'DELETE',
            headers: {
                "Authorization": auth_token,
            },
        })
            .then(response => {
                if (!response.ok) {
                    throw Error();
                }
                return response.json();
            })
            .then(data => {
                if (data.status === "ok") {
                    $('.b_binds button:contains("' + bindName + '")').remove();
                    $('#b_delete li:contains("' + bindName + '")').remove();
                    $('#b_delete_button_select > ul  li').first().click();
                    return;
                }
                showNotification(data.message);
            })
            .catch(error => {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            });
    });

    $('.b_binds button').on('click', sendMessage);
});