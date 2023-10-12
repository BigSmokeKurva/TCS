const loginButton = document.getElementById('login-header');
const registerButton = document.getElementById('register-header');
const underline = document.getElementById('underline');

function updateUnderline(element) {
    const buttonRect = element.getBoundingClientRect();
    const containerRect = document.querySelector('.button-container').getBoundingClientRect();
    const leftOffset = buttonRect.left - containerRect.left;

    underline.style.width = buttonRect.width + 32 + 'px';
    underline.style.left = leftOffset - 16 + 'px';
}

function clickAuth() {
    if (!(validateLogin() | validatePassword()))
        return;
}

function clickReg() {
    if (!(validateEmail() | validateLogin() | validatePassword()))
        return;

    var email = $('#email').val();
    var login = $('#login').val();
    var password = $('#password').val();

    var requestData = {
        email: email,
        login: login,
        password: password
    };

    $.ajax({
        type: "POST",
        url: "/api/registration",
        data: JSON.stringify(requestData), // Преобразуем данные в JSON
        contentType: "application/json", // Устанавливаем заголовок Content-Type
        success: function (response) {
            // Обработка успешного ответа от сервера
            console.log("Регистрация выполнена успешно.", response);
        },
        error: function (error) {
            // Обработка ошибки
            console.error("Произошла ошибка при регистрации.", error);
        }
    });
}

function validateEmail() {
    var email = $('#email').val();
    var emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/;
    var isValid = emailPattern.test(email);

    // Конфигурация для управления элементами
    var config = {
        continueButton: $('.continue-button'),
        emailInput: $('#email')
    };

    if (isValid) {
        // Валидный
        config.continueButton.prop('disabled', false);
        config.emailInput.removeClass('invalid-input');
        return true;
    }
    // Невалидный
    config.continueButton.prop('disabled', true);
    config.emailInput.addClass('invalid-input');
    return false;
}

function validateLogin() {
    var login = $('#login').val();
    var loginPattern = /^[a-zA-Z0-9_-]+$/;
    var isValid = loginPattern.test(login);

    // Конфигурация для управления элементами
    var config = {
        continueButton: $('.continue-button'),
        loginInput: $('#login')
    };

    if (isValid) {
        // Валидный
        config.continueButton.prop('disabled', false);
        config.loginInput.removeClass('invalid-input');
        return true;
    }
    // Невалидный
    config.continueButton.prop('disabled', true);
    config.loginInput.addClass('invalid-input');
    return false;
}

function validatePassword() {
    var password = $('#password').val();
    var minLength = 5; // Минимальная длина пароля (в данном случае 8 символов)
    var passwordPattern = /^[a-zA-Z0-9!@#$%^&*()_-]+$/;
    var isValid = password.length >= minLength && passwordPattern.test(password);

    // Конфигурация для управления элементами
    var config = {
        continueButton: $('.continue-button'),
        passwordInput: $('#password')
    };

    if (isValid) {
        // Валидный
        config.continueButton.prop('disabled', false);
        config.passwordInput.removeClass('invalid-input');
        return true;
    }
    // Невалидный
    config.continueButton.prop('disabled', true);
    config.passwordInput.addClass('invalid-input');
    return false;
}

loginButton.addEventListener('click', () => {
    updateUnderline(loginButton);
    loginButton.classList.add("button-active");
    registerButton.classList.remove("button-active");
    var form =
        $('<input>', { id: 'login', placeholder: 'логин' }).on('blur', validateLogin).add(
            $('<input>', { id: 'password', placeholder: 'пароль', type: 'password' }).on('blur', validatePassword)).add(
                $('<button>', { class: 'continue-button', text: 'ВОЙТИ' }).click(
                    clickAuth
                )
            );

    $('#main-form').html(form);
});

registerButton.addEventListener('click', () => {
    updateUnderline(registerButton);
    registerButton.classList.add("button-active");
    loginButton.classList.remove("button-active");
    var form =
        $('<input>', { id: 'email', placeholder: 'почта' }).on('blur', validateEmail).add(
            $('<input>', { id: 'login', placeholder: 'логин' }).on('blur', validateLogin)).add(
                $('<input>', { id: 'password', placeholder: 'пароль', type: 'password' }).on('blur', validatePassword)).add(
                    $('<button>', { class: 'continue-button', text: 'РЕГИСТРАЦИЯ' }).click(
                        clickReg
                    )
                );

    $('#main-form').html(form);
});

// Обновляем подчеркивание при загрузке страницы.
window.addEventListener('load', () => {
    loginButton.click();
});
