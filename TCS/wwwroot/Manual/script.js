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
            if (data.status === 'ok') {
                return;
            }
            showNotification(data.message);
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });


}

$(document).ready(function () {
    let input = $('.m_full-width-input');
    const maxLength = 1000; // Максимальное количество символов

    input.on('input', function () {
        var $textarea = $(this);
        $textarea.css('height', 'auto');
        $textarea.css('height', $textarea.prop('scrollHeight') + 'px');

        // Обрезаем текст, если он превышает лимит символов
        const text = $textarea.val();
        if (text.length > maxLength) {
            $textarea.val(text.substring(0, maxLength));
        }
    });

    input.on("keydown", function (event) {
        if (event.key === "Enter") {
            event.preventDefault();
            $('.send-button').click();
        }
    });

    input.on("paste", function (event) {
        event.preventDefault();

        const pastedText = (event.originalEvent.clipboardData || window.clipboardData).getData("text");
        const cleanText = pastedText.replace(/\n/g, '');
        const $textarea = $(this);
        const start = this.selectionStart;

        // Обрезаем текст, если он превышает лимит символов
        const currentText = $textarea.val();
        const newText = currentText.slice(0, start) + cleanText + currentText.slice(this.selectionEnd);
        if (newText.length > maxLength) {
            $textarea.val(newText.substring(0, maxLength));
        } else {
            $textarea.val(newText);
        }
        $textarea.css('height', 'auto');
        $textarea.css('height', $textarea.prop('scrollHeight') + 'px');
        // Устанавливаем позицию курсора
        $textarea.prop({ selectionStart: start + cleanText.length, selectionEnd: start + cleanText.length });
    });

    $('.back-button').on('click', function () {
        $('.m_full-width-input').val(lastMessage);
    });

    $('.send-button').on('click', function () {
        if (isRandom) {
            nextBtn();
        }
        var bot = $('#bots-list #bots [class="item selected-item"]');
        if (bot.length === 0) {
            return;
        }
        var botname = bot.attr('botname');
        var message = $('.m_full-width-input').val();
        if (message.replace(/\s/g, '').length === 0) {
            showNotification("Для отправки, введите сообщение.");
            return;
        }
        const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
        fetch('api/app/sendmessage', {
            method: 'POST',
            headers: {
                "Authorization": auth_token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                botname: botname,
                message: message
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw Error();
                }
                return response.json();
            })
            .then(data => {
                if (data.status === "error") {
                    showNotification(data.message);
                } else {
                    lastMessage = $('.m_full-width-input').val();
                    $('.m_full-width-input').val('');
                }
            })
            .catch(error => {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            });

    });

    $('.b_binds button').on('click', sendMessage);

});
