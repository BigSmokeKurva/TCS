
function toggleSpam() {
    var btn = $(this);
    var state = btn.attr('state');
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    if (state === 'started') {
        fetch("/api/app/stopSpam", {
            method: "Get",
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
                    btn.attr('state', 'stoped');
                    btn.text('Начать');
                    return;
                }
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            })
            .catch(error => {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            });

    } else {
        var data = {
            threads: parseInt($('#s_count-bots > input').val()),
            delay: parseInt($('#s_delay > input').val()),
            messages: $('.s_textarea').val().split('\n'),
            mode: $('#s_random-btn').hasClass('selected') ? 0 : 1
        };
        fetch("/api/app/startSpam", {
            method: "Post",
            headers: {
                "Authorization": auth_token,
                "Content-Type": "application/json"
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
                    btn.attr('state', 'started');
                    btn.text('Остановить');
                    return;
                }
                showNotification(data.message);
            })
            .catch(error => {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            });

    }
}

function toggleSpamModeMenu() {
    var $optionSelectedOption = $('#s_option_select > div');
    var $optionOptions = $('#s_option_select > ul');
    $optionOptions.is(':visible') ? $optionOptions.fadeOut(100) : $optionOptions.fadeIn(100);
    $optionSelectedOption.toggleClass('selected-option-active');
    $optionSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

function setOption(btn) {
    var parent = btn.parent();
    $('.s_options_content > div').hide();
    parent.find('li.selected').removeClass('selected');
    btn.addClass('selected');
    $('#s_option_select > div > span').text(btn.text());
}

$(document).ready(function () {
    const numberInput = $('.s_number-input');

    numberInput.on('input', function () {
        var text = $(this).val();
        var max = $(this).attr('max');
        var maxInt = parseInt(max);
        if (text[0] === '0') {
            $(this).val('');
        }
        const value = parseInt($(this).val());
        if (isNaN(value) || value < 0) {
            $(this).val('');
        } else if (value > maxInt) {
            $(this).val(max);
        }
    });

    numberInput.on('paste', function (e) {
        e.preventDefault();
    });

    $('#s_save').on('click', function () {
        var data = {
            threads: parseInt($('#s_count-bots > input').val()),
            delay: parseInt($('#s_delay > input').val()),
            messages: $('.s_textarea').val().split('\n')
        };
        var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

        fetch("/api/app/updateSpamConfiguraion", {
            method: "POST",
            headers: {
                "Authorization": auth_token,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(data),
        })
            .then(response => {
                if (!response.ok) {
                    throw Error();
                }
                return response.json();
            })
            .then(data => {
                if (data.status === "ok") {
                    showNotification("Настройки сохранены.");
                    var startButton = $('#s_start-stop');
                    if (startButton.attr('state') === 'started') {
                        startButton.attr('state', 'stoped');
                        startButton.text('Начать');
                    };
                    return;
                }
                showNotification(data.message);
            })
            .catch(error => {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            });
    });
    $('#s_start-stop').on('click', toggleSpam);
    $('#s_option_select > div').on('click', function (e) {
        e.stopPropagation();
        toggleSpamModeMenu();
    });
    $('#s_random-btn').on('click', function () {
        $('#s_count-bots > span').text('Потоки');
        toggleSpamModeMenu();
        setOption($(this));
    });
    $('#s_list-btn').on('click', function () {
        $('#s_count-bots > span').text('Боты');
        toggleSpamModeMenu();
        setOption($(this));
    });
    setOption($('#s_random-btn'));
    $('#s_count-bots > span').text('Потоки');

});
