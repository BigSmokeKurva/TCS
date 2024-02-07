let selectedButton = null;
let users = {};
let selectedUser = null;
let selectedUserInfo = null;
let editorTarget = null;
const search = $('.search > input');
const $logsSelectedOption = $('#logs-select > .selected-option');
const $logsOptions = $('#logs-select > .options');
const notificationContainer = document.getElementById('notification-container');
const chatLogsButton = document.getElementById('chat-logs');
const actionsLogsButton = document.getElementById('actions-logs');
const underline = document.getElementById('underline');
const propertyMappings = {
    'username': 0,
    'password': 1,
    'email': 2,
    'admin': 3,
    'tokens': 4,
    'paused': 5
};

function showNotification(notificationText) {
    const notification = document.createElement('div');
    notification.className = 'notification';
    const notificationContent = document.createElement('div');
    notificationContent.className = 'notification-content';
    const image = document.createElement('img');
    image.src = '/Authorization/error.svg';
    image.alt = 'Ошибка';
    // Создаем элемент для текста
    const text = document.createElement('div');
    text.textContent = notificationText;
    notificationContent.appendChild(image);
    notificationContent.appendChild(text);

    notification.appendChild(notificationContent);
    const existingNotifications = notificationContainer.querySelectorAll('.notification');

    existingNotifications.forEach(existingNotification => {
        existingNotification.style.bottom = parseInt(existingNotification.style.bottom) + 50 + 'px';
    });

    notificationContainer.appendChild(notification);
    notification.style.bottom = '0';

    // Добавляем класс "show" для анимации появления
    setTimeout(() => {
        notification.classList.add('show');
    }, 0);

    // Задержка перед началом анимации исчезновения
    setTimeout(() => {
        notification.classList.add('hide');

        // Удаление уведомления после окончания анимации
        notification.addEventListener('transitionend', () => {
            notification.remove();
        });
    }, 3000); // Изменено на 3000 миллисекунд для удобства демонстрации

}

function updateUnderline(element) {
    const buttonRect = element.getBoundingClientRect();
    const containerRect = document.querySelector('.button-container.logs_title').getBoundingClientRect();
    const leftOffset = buttonRect.left - containerRect.left;

    underline.style.width = buttonRect.width + 32 + 'px';
    underline.style.left = leftOffset - 16 + 'px';
}

async function getInfoForSelectedUser() {
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/getuserinfo?id=' + selectedUser.id, {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return null; // или верните значение по умолчанию
        }

        const data = await response.json();
        return data;
    } catch (error) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        return null; // или верните значение по умолчанию
    }
}

async function getLogs(type) {
    var selectedTime = $logsSelectedOption.find('span').text();
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    var response = await fetch('api/admin/getlogs?id=' + selectedUser.id + '&time=' + selectedTime + '&type=' + type, {
        method: 'GET',
        headers: {
            "Authorization": auth_token
        }
    });

    if (!response.ok) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
    }

    var data = await response.json();
    var logsContainer = $("#logs > div > div.list.list-logs");
    logsContainer.empty();

    data.forEach(log => {
        var logTime = new Date(log.time);
        var formattedTime = logTime.toLocaleTimeString();

        var logDiv = $("<div>").addClass("logs_line");
        var timeSpan = $("<span>").text(formattedTime);
        var messageSpan = $("<span>").text(log.message);

        logDiv.append(timeSpan, messageSpan);
        logsContainer.append(logDiv);

        var underlineDiv = $("<div>").addClass("logs_underline");
        logsContainer.append(underlineDiv);
    });

}

async function selectUser(button) {
    if (selectedButton) {
        selectedButton.removeAttribute("style");
        selectedButton.disabled = false;
    }
    button.style.backgroundColor = "rgb(118, 104, 203)";
    button.disabled = true;
    selectedButton = button;

    var user_id = parseInt(button.getAttribute("user_id"));
    selectedUser = users[user_id];
    selectedUserInfo = await getInfoForSelectedUser();

    $('#user-username').text(selectedUser.username);
    $('#password').text(selectedUserInfo.password);
    $('#tokens-count').text(selectedUserInfo.tokensCount);
    $('#email').text(selectedUser.email);
    $("#admin-btn").text(selectedUserInfo.admin ? "Снять админку" : "Выдать админку");
    $('#pause-btn').text(selectedUserInfo.paused ? "Снять паузу" : "Поставить на паузу");

    const logsTimeList = $('#logs-time-list');
    logsTimeList.empty();

    // Используем метод `map` для создания массива элементов <li>
    const liElements = selectedUserInfo.logsTime.map(timestamp => $('<li>').text(timestamp));
    logsTimeList.append(liElements);

    // Создаем jQuery-коллекцию из массива элементов и добавляем обработчик событий
    logsTimeList.on('click', 'li', async function () {
        logsTimeList.find('li.selected').removeClass('selected');
        $(this).addClass('selected');
        $logsSelectedOption.find('span').text($(this).text());
        toggleLogs();
        if (chatLogsButton.classList.contains('button-active'))
            var type = 0;
        else if (actionsLogsButton.classList.contains('button-active'))
            var type = 1;

        await getLogs(type);
    });

    // Выбираем первый элемент и обновляем отображение
    const firstLi = logsTimeList.find('li:first');
    firstLi.addClass('selected');
    $logsSelectedOption.find('span').text(firstLi.text());
    $("#info-grid").show();

}

function getUsers() {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    fetch('api/admin/getusers', {
        method: 'GET',
        headers: {
            "Authorization": auth_token
        }
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            var usersList = $('#users.list');
            usersList.empty();

            data.forEach(user => {
                var btn = $('<button>', { user_id: user.id }).text(user.username).on('click', async function () {
                    await selectUser(this);
                    if (chatLogsButton.disabled !== true) {
                        chatLogsButton.click();
                    }
                    else {
                        await getLogs(0);
                    }
                });
                users[user.id] = user;

                var item = $('<div>').addClass('item').append(btn);
                usersList.append(item);
            });
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}

function searchUser() {
    var searchText = search.val().toLowerCase();


    if (searchText === "") {
        $('.item > button[style="display: none;"]').show();
        return;
    }
    $('.item > button').hide();

    var matchingKeys = [];

    for (var key in users) {
        if (users.hasOwnProperty(key)) {
            var user = users[key];
            var username = user.username.toLowerCase();
            var email = user.email.toLowerCase();

            if (username.includes(searchText) || email.includes(searchText)) {
                matchingKeys.push(key);
            }
        }
    }

    // Показать только кнопки с соответствующими user_id
    matchingKeys.forEach(function (userId) {
        $('.item > button[user_id="' + userId + '"]').show();
    });

}

function toggleLogs() {
    $logsOptions.is(':visible') ? $logsOptions.fadeOut(100) : $logsOptions.fadeIn(100);
    $logsSelectedOption.toggleClass('selected-option-active');
    $logsSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

async function sendEditUserRequest(property, value) {
    property = propertyMappings[property];
    var data = {
        id: selectedUser.id,
        property: property,
        value: value
    };
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/edituser', {
            method: 'POST',
            headers: {
                "Authorization": auth_token,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return false;
        }

        const responseData = await response.json();
        if (responseData.status !== 'ok') {
            showNotification(responseData.message);
            return false;
        }

        return responseData;
    } catch (error) {
        // Обработка ошибки
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        return false;
    }
}

async function editUserEditable(selector, property) {
    var input = $(selector);
    var img = input.siblings('button').find('img');

    if (input.attr('contenteditable') === 'true') {
        var value = input.text();
        if (!await sendEditUserRequest(property, value)) {
            return false;
        }
        input.attr('contenteditable', 'false');
        img.attr('src', '/App/edit.svg');
        return true;
    }

    input.attr('contenteditable', 'true');
    img.attr('src', '/App/save.svg');
    return true;
}

function uploadTokens(input) {
    var selectedFile = input[0].files[0];
    if (!selectedFile) {
        return;
    }

    var reader = new FileReader();

    reader.onload = async function (e) {
        var fileContent = e.target.result;
        var lines = fileContent.split('\n')
            .map(line => line.trim().replace(/\r/g, ''))
            .filter(line => line !== '');

        try {
            showNotification("Проверка токенов...");
            var response = await sendEditUserRequest('tokens', lines);
            $('#tokens-count').text(response.message);
            showNotification("Токены проверены.");
        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    };

    reader.readAsText(selectedFile);
}

function download(data, fileName) {
    // Создаем новый Blob (бинарный объект) с вашими данными и типом MIME "text/plain"
    const blob = new Blob([data], { type: "text/plain" });

    // Создаем ссылку для скачивания
    const url = window.URL.createObjectURL(blob);

    // Создаем элемент "a" для скачивания файла
    const a = $("<a>")
        .attr("href", url)
        .attr("download", fileName); // Имя файла, которое будет предложено при скачивании

    // Добавляем элемент "a" в документ и производим клик на нем для начала загрузки
    a.appendTo("body").get(0).click();

    // Освобождаем ресурсы после завершения скачивания
    window.URL.revokeObjectURL(url);

    // Удаляем элемент "a" после скачивания
    a.remove();
}

async function downloadTokens() {
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/gettokens?id=' + selectedUser.id, {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return;
        }
        const data = await response.json();

        //let resultString = "";

        //// Перебираем элементы словаря
        //for (const key in data) {
        //    if (data.hasOwnProperty(key)) {
        //        resultString += key + ":" + data[key] + "\n";
        //    }
        //}

        //download(resultString, 'tokensAndUsernames.txt');
        //resultString = "";

        //// Перебираем элементы словаря
        //for (const key in data) {
        //    if (data.hasOwnProperty(key)) {
        //        resultString += key + "\n";
        //    }
        //}

        download(data.join('\n'), 'tokens.txt');

    } catch (error) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
    }
}


function setupFileInput(input, uploadFunction) {
    input.off('change');
    input.change(function () {
        uploadFunction(input);
    });
    input.click();
    input.val('');
}

function uploadFilterWords(input) {
    var selectedFile = input[0].files[0];
    if (!selectedFile) {
        return;
    }

    var reader = new FileReader();

    reader.onload = async function (e) {
        var fileContent = e.target.result;
        var lines = fileContent.split('\n')
            .map(line => line.trim().replace(/\r/g, ''))
            .filter(line => line !== '');

        try {
            const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

            var response = await fetch('api/admin/uploadfilter', {
                method: 'POST',
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": auth_token
                },
                body: JSON.stringify(lines)
            });

            if (!response.ok) {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
                return;
            }
        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    };

    reader.readAsText(selectedFile);
}

function downloadFilterWords() {
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    fetch('api/admin/getfilter', {
        method: 'GET',
        headers: {
            "Authorization": auth_token
        }
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            let resultString = "";

            data.forEach(word => {
                resultString += word + "\n";
            });

            download(resultString, 'filter.txt');
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}

function showEditor(target, list) {
    $('#editor-container, .overlay').fadeIn(50);
    editorTarget = target;
    $('#editor-textarea').focus();
    $('#editor-textarea').val(list.join('\n'));
}

function cancelEditor() {
    $('#editor-container, .overlay').fadeOut(50);
    editorTarget = null;
    $('#editor-textarea').val('');
    $(document).focus();
}

async function saveEditor() {
    var text = $('#editor-textarea').val();
    var lines = text.split('\n')
        .map(line => line.trim().replace(/\r/g, ''))
        .filter(line => line !== '');

    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    var url;
    switch (editorTarget) {
        case 'filter':
            url = 'api/admin/uploadfilter';
            break;
        case 'tokens':
            cancelEditor();
            showNotification("Проверка токенов...");
            var response = await sendEditUserRequest('tokens', lines);
            $('#tokens-count').text(response.message);
            showNotification("Токены проверены.");
            return;
    }
    var response = await fetch(url, {
        method: 'POST',
        headers: {
            "Authorization": auth_token,
            "Content-Type": "application/json"
        },
        body: JSON.stringify(lines)
    });

    if (!response.ok) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        return;
    }

    switch (editorTarget) {
        case 'filter':
            showNotification("Фильтр успешно обновлен.");
            break;
    }

    cancelEditor();
}

$(document).ready(function () {
    getUsers();
    search.on('input', searchUser);
    $("#exit-button").on("click", function () {
        fetch("/api/auth/unauthorization", {
            method: "GET"
        })
            .then(response => {
                if (!response.ok) {
                    throw Error();
                }
                return response.json();
            })
            .then(data => {
                if (data.status === "ok") {
                    window.location.href = "/";
                }
            })
            .catch(error => {
                // Обработка ошибки
            });
    });
    $logsSelectedOption.on('click', function (e) {
        e.stopPropagation();
        toggleLogs();
    });
    $(document).on('click', function (event) {
        if (!$(event.target).is($logsSelectedOption) && !$.contains($logsOptions[0], event.target) && $logsOptions.is(':visible')) {
            toggleLogs();
        }
    });
    $('#email + button').on('click', async function () {
        if (await editUserEditable('#email', 'email')) {
            selectedUser.email = $('#email').text();
            users[selectedUser.id].email = selectedUser.email;
        }
    });
    $('#user-username + button').on('click', async function () {
        if (await editUserEditable('#user-username', 'username')) {
            selectedUser.username = $('#user-username').text();
            users[selectedUser.id].username = selectedUser.username;
            $('#users > div > button[disabled]').text(selectedUser.username);
        }
    });
    $('#password + button').on('click', function () {
        editUserEditable('#password', 'password');
        selectedUserInfo.password = $('#password').text();
    });
    $('#info-panel span[contenteditable]').on('keydown', function (e) {
        if (e.keyCode == 13) {
            e.preventDefault();
            $(this).find('+ button').click();
        }
    })
    $('#admin-btn').on('click', async function () {
        var value = !selectedUserInfo.admin;
        await sendEditUserRequest('admin', value);
        selectedUserInfo.admin = value;
        $("#admin-btn").text(value ? "Снять админку" : "Выдать админку");
    });
    $('#pause-btn').on('click', async function () {
        var value = !selectedUserInfo.paused;
        await sendEditUserRequest('paused', value);
        selectedUserInfo.paused = value;
        $('#pause-btn').text(value ? "Снять паузу" : "Поставить на паузу");
    });
    $('#delete-user-btn').on('click', async function () {
        const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

        try {
            const response = await fetch('api/admin/deleteuser?id=' + selectedUser.id, {
                method: 'DELETE',
                headers: {
                    "Authorization": auth_token
                }
            });

            if (!response.ok) {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
                return;
            }

            const data = await response.json();

            if (data.status !== 'ok') {
                throw new Error();
            }

            $('#users > div > button[disabled]').remove();
            delete users[selectedUser.id];
            selectedUserInfo = null;
            selectedUser = null;
            $("#info-grid").hide();
        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    });
    chatLogsButton.addEventListener('click', async function () {
        updateUnderline(chatLogsButton);
        chatLogsButton.classList.add('button-active');
        chatLogsButton.disabled = true;
        actionsLogsButton.classList.remove('button-active');
        actionsLogsButton.disabled = false;
        await getLogs(0);
    });
    actionsLogsButton.addEventListener('click', async function () {
        updateUnderline(actionsLogsButton);
        actionsLogsButton.classList.add('button-active');
        actionsLogsButton.disabled = true;
        chatLogsButton.classList.remove('button-active');
        chatLogsButton.disabled = false;
        await getLogs(1);
    });
    $('.menu-item .toggleButton').click(function (event) {
        event.stopPropagation();

        var $clickedMenuItem = $(this).closest('.menu-item');
        var $dropdownContent = $clickedMenuItem.find('.dropdown-content');

        $('.menu-item').not($clickedMenuItem).find('.dropdown-content').hide();

        $dropdownContent.toggle();
    });
    $(document).on('click', function (event) {
        var $dropdownContents = $('.dropdown-content');
        var $clickedMenuItem = $(event.target).closest('.menu-item');

        if (!$dropdownContents.is(event.target) && $dropdownContents.has(event.target).length === 0 &&
            !$clickedMenuItem.length) {
            $dropdownContents.hide();
        }
    });
    $('.menu-item .dropdown-content button').click(function () {
        $(this).closest('.dropdown-content').hide();
    });
    $('#upload-filter').on('click', function () {
        setupFileInput($('#fileInput'), uploadFilterWords);
    });
    $('#download-filter').on('click', function () {
        downloadFilterWords();
    });
    $('#cancel-editor-btn').on('click', function () {
        cancelEditor();
    });
    $('#edit-filter').on('click', async function () {
        var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
        var response = await fetch('api/admin/getfilter', {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return;
        }

        var data = await response.json();
        showEditor('filter', data);
    });
    $('#save-editor-btn').on('click', saveEditor);
    $('#edit-tokens').on('click', async function () {
        var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
        var response = await fetch('api/admin/gettokens?id=' + selectedUser.id, {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return;
        }

        var data = await response.json();
        showEditor('tokens', data);
    });
    $('#upload-tokens').on('click', function () {
        setupFileInput($('#fileInput'), uploadTokens);
    });
    $('#download-tokens').on('click', function () {
        downloadTokens();
    });
});


