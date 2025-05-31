/*const apiUrl = "http://localhost:8080";  // 後端的 URL*/
const apiUrl = ""
let connection = undefined;

let currentUser = null;
let currentChatType = null;  // "private" 或 "group"
let currentChatId = null;    // 私聊是帳號，群聊是群組名稱

// 假資料測試用
let privateChats = { };  // 以對方帳號為 key，內容為訊息陣列 [{sender:'xxx',text:'yyy'}]
let groupChats = { };    // 以群組名稱為 key，內容為訊息陣列 [{sender:'xxx',text:'yyy'}]

let selectedLi = null;

function handleLiSelection(li) {
    if (selectedLi) selectedLi.classList.remove("selected");
    li.classList.add("selected");
    selectedLi = li;
}

function UserListHandleClientEvent() {
    document.getElementById("userList").addEventListener("click", (e) => {
        if (e.target.tagName === "LI") {
            handleLiSelection(e.target);
            openChat("private", e.target.textContent);
        }
    });
}
function GroupListHandleClientEvent() {
    document.getElementById("groupList").addEventListener("click", (e) => {
        if (e.target.tagName === "LI") {
            handleLiSelection(e.target);
            joinGroup(e.target.textContent);
            openChat("group", e.target.textContent);
        }
    });
}


// 防止XSS
function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}

function getUsername() {
    return document.getElementById("username").value;
}
function getPassword() {
    return document.getElementById("password").value;
}

function handleKeyDown(e) {
    if (e.key === 'Enter') {
        sendMessage();
    }
}

// 清除聊天視窗
function clearChatArea() {
    //document.getElementById("chatTitle").innerText = "No Chat Partner Selected";
    document.getElementById("chatMessages").innerHTML = "";
    document.getElementById("chatInput").value = "";
}


// 切換左側聊天列表 (私聊或群聊)
function switchChatList(type) {
    //currentChatType = null; // 切換列表先不選聊天對象
    //currentChatId = null;
    clearChatArea();

    if (type === "private") {
        document.getElementById("privateList").classList.remove("hidden");
        document.getElementById("groupListContainer").classList.add("hidden");
        document.getElementById("tabPrivateBtn").classList.add("border-cyan-400", "text-white");
        document.getElementById("tabPrivateBtn").classList.remove("text-gray-400");
        document.getElementById("tabGroupBtn").classList.remove("border-cyan-400", "text-white");
        document.getElementById("tabGroupBtn").classList.add("text-gray-400");
    } else {
        document.getElementById("privateList").classList.add("hidden");
        document.getElementById("groupListContainer").classList.remove("hidden");
        document.getElementById("tabGroupBtn").classList.add("border-cyan-400", "text-white");
        document.getElementById("tabGroupBtn").classList.remove("text-gray-400");
        document.getElementById("tabPrivateBtn").classList.remove("border-cyan-400", "text-white");
        document.getElementById("tabPrivateBtn").classList.add("text-gray-400");
    }
}

// 送出訊息
function sendMessage() {
    if (!currentUser || !currentChatType || !currentChatId) {
        alert("Please Select a Chat Partner First");
        return;
    }
    const input = document.getElementById("chatInput");
    const msg = input.value.trim();
    if (!msg) return;
    input.value = "";

    // 儲存訊息
    const chatLog = (currentChatType === "private" ? privateChats : groupChats)[currentChatId];
    if (chatLog) {
        const newMsg = { sender: currentUser, text: msg };
        chatLog.push(newMsg);
        appendMessage(newMsg);
    } else {
        alert("Chat History Not Found");
        return;
    }
    openChat(currentChatType, currentChatId);
}

// 新增群聊
function createGroup() {
    const input = document.getElementById("newGroupName");
    const groupName = input.value.trim();
    if (!groupName) {
        console.error("請輸入群組名稱");
        return;
    }

    //if (groupChats[groupName]) {
    //    console.error("重複群組名稱");
    //    return;
    //}

    groupChats[groupName] = [];
    input.value = "";


    //// 呼叫 Hub 方法 加入聊天室
    //connection.invoke("CreateRoomAsync", groupName)
    //    .then((res) =>
    //    {
    //        if (!res.ok) {
    //            console.error("Failed to Create Group Chat");
    //            return;
    //        }
    //        // 返回 roomId
    //        // 已加入聊天室
    //        console.log(`Group ${groupName} Created Successfully`);
    //    })
    //    .catch(err => console.error(err));

    joinGroup();
    renderGroupChatList();
}
// 加入群組
function joinGroup(groupName) {
    // 預設已在群組名單裡，可擴充此處加入伺服器或其他行為
}
// 離開群組
function leaveGroup() {
    if (currentChatType !== "group" || !currentChatId) {
        alert("Not in Any Group Chat");
        return;
    }
    delete groupChats[currentChatId];
    currentChatType = null;
    currentChatId = null;
    clearChatArea();
    renderGroupChatList();
}

// 私聊列表
function renderPrivateChatList() {

    const userList = document.getElementById("userList");
    userList.innerHTML = "";

    for (const user in privateChats) {
        const li = document.createElement("li");
        li.textContent = user;
        li.className = "chat-list-item px-4 py-2 text-gray-300 hover:text-white";
        if (currentChatType === "private" && currentChatId === user) {
            li.classList.add("selected");
        }
        userList.appendChild(li);
    }
}

// 群聊列表
function renderGroupChatList() {
    const groupList = document.getElementById("groupList");
    groupList.innerHTML = "";

    for (const group in groupChats) {
        const li = document.createElement("li");
        li.textContent = group;
        li.className = "chat-list-item px-4 py-2 text-gray-300 hover:text-white";
        if (currentChatType === "group" && currentChatId === group) {
            li.classList.add("selected");
        }
        groupList.appendChild(li);
    }
}

// 渲染聊天名單 (私聊 + 群聊)
function renderChatLists() {
    renderPrivateChatList();
    renderGroupChatList();
}

function renderMessage(msg) {
    const div = document.createElement("div");
    div.classList.add("message-item");

    if (msg.sender === currentUser) {
        div.classList.add("my-message");
        div.textContent = escapeHtml(msg.text);
    } else {
        div.classList.add("other-message");
        div.innerHTML = `<span class="chat-sender">${msg.sender}</span>: ${escapeHtml(msg.text)}`;
    }

    return div;
}


function appendMessage(msg) {
    const messagesDiv = document.getElementById("chatMessages");
    messagesDiv.appendChild(renderMessage(msg));
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}


// 開啟聊天對話
function openChat(type, id) {

    // 避免同一個對話重複render
    if (currentChatType === type && currentChatId === id) {
        return;
    }

    currentChatType = type;
    currentChatId = id;
    document.getElementById("chatTitle").innerText = (type === "private" ? " " : " ") + id;

    // 讀取對話內容
    let chatLog = (type === "private" ? privateChats : groupChats)[id] || [];
    const messagesDiv = document.getElementById("chatMessages");
    messagesDiv.innerHTML = "";

    console.log("type: " + type);
    console.log("id: " + id);

    console.log(chatLog);
    chatLog.forEach(msg => {
        messagesDiv.appendChild(renderMessage(msg));
    });

    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}



function showContainer(hostName, containerIP) {
    const el = document.getElementById("containerInfoDisplay");
    el.textContent = `Container Id: ${hostName} IP: ${containerIP}`;
    el.classList.remove("hidden");
}

// 顯示登入狀態的 UI
function showLoggedInState(username, userId) {

    currentUser = userId;

    document.getElementById("userDisplay").innerText = `Hello ${username}`;
    document.getElementById("userDisplay").classList.remove("hidden");

    // 隱藏登入，顯示聊天介面
    document.getElementById("authContainer").classList.add("hidden");
    document.getElementById("logoutBtn").classList.remove("hidden");
    document.getElementById("chatContainer").classList.remove("hidden");

    // 初始切換至私聊列表
    switchChatList("private");

    // 預設載入空白聊天
    clearChatArea();

    // 模擬初始私聊群組名單
    for (let i = 0; i < 30; i++) {
        privateChats["你的朋友" + (i + 1)] = []
        privateChats["你的朋友" + (i + 1)].push({ sender: "你的朋友" + (i + 1), text: "This is msg" + (i + 1) });
    }

    for (let i = 0; i < 3; i++) {
        groupChats["工程"+(i+1)] = []
        groupChats["工程"+(i+1)].push({ sender: "gang", text: "This is msg" + (i+1) })

    }

    UserListHandleClientEvent();
    GroupListHandleClientEvent();


    renderChatLists();
}

// 顯示登出狀態的 UI
function showLoggedOutState() {

    currentUser = null;
    currentChatType = null;
    currentChatId = null;

    document.getElementById("userDisplay").classList.add("hidden");
    document.getElementById("logoutBtn").classList.add("hidden");
    document.getElementById("chatContainer").classList.add("hidden");
    document.getElementById("authContainer").classList.remove("hidden");

    // 清空聊天列表與訊息
    document.getElementById("userList").innerHTML = "";
    document.getElementById("groupList").innerHTML = "";
    clearChatArea();

    getContainerInfo("");
}

function fetchWithAuth(input, init = {}) {
    init.credentials = 'include'; // 確保 cookie 被送出
    return fetch(input, init)
        .then(response => {
            if (response.status !== 401) {
                // Token 沒過期，正常回傳
                return response;
            }
            return fetch('/api/auth/refresh', {
                method: 'POST',
                credentials: 'include', // 這裡也要加，否則 cookie 不會帶過去
            })
            .then(refreshResponse => {
                if (!refreshResponse.ok) {
                    return Promise.reject('refresh token 更新失敗');
                }
                return refreshResponse.json()
            }).then(data => {
                // Refresh 成功，再次嘗試原始請求
                // 儲存新的 jwt
                document.cookie = `jwt=${data.token}; path=/`;
                // 重試原始請求
                return fetch(input, init);
            });
        }); 
}

function getContainerInfo(containerInfo) {
    if (!containerInfo.hostName) {
        fetch(`${apiUrl}/api/auth/containerinfo`)
            .then(res => res.json())
            .then(data => {
                showContainer(data.hostName, data.containerIP);
            })
            .catch(err => console.error("取得 container info 失敗", err));
    } else {
        showContainer(containerInfo.hostName, containerInfo.containerIP);
    }
    
}

// 註冊
function register() {
    fetch(`${apiUrl}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: getUsername(), password: getPassword() })
    }).then(res => {
        if (res.ok) console.log("註冊成功");
        else res.text().then(console.error);
    });
}

// 當用戶登入後，儲存 JWT
function login() {

    fetch(`${apiUrl}/api/auth/login`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ username: getUsername(), password: getPassword() })
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => {
                    return Promise.reject(err);
                });
            }
            return response.json()
        })
        .then(data => {
            document.cookie = `jwt=${data.token}; path=/`;  // 儲存 JWT
            document.cookie = `refresh=${data.refreshToken}; path=/`; // 儲存 refresh token
            console.log("登入成功");
            const payload = parseJwt(data.token);
            const userId = payload?.["UserId"];
            showLoggedInState(getUsername(), userId);
            connectToSignalR();
        })
        .catch(err => console.error(err.message));
}

// 登出
function logout() {
    fetch(`${apiUrl}/api/auth/logout`, {
        method: 'POST',
        ///credentials: 'include'
    }).then(() => {
        document.cookie = "jwt=; Max-Age=0; path=/"; // 刪除瀏覽器中的 cookie。
        document.cookie = "refresh=; Max-Age=0; path=/";
        console.log("已登出");
        showLoggedOutState();
        // todo 登出 自動斷線
    });
}

// 註冊 SignalR 連線並加入 JWT 認證
function connectToSignalR() {
    //const token = getCookie("jwt");  // 從 cookie 獲取 JWT

    if (connection) {
        connection.stop(); // 清除舊連線
        connection = undefined;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`${apiUrl}/chatHub`, {
            transport: signalR.HttpTransportType.WebSockets, // 強制 WebSocket
            accessTokenFactory: () => getCookie("jwt"), // 設定 JWT
            skipNegotiation: true
        })
        .withAutomaticReconnect([5000, 5000, 5000]) // 每次間隔 5 秒，最多 3 次
        //.withServerTimeout(60000) // 如果伺服器未在此間隔中傳送訊息，客戶端會考慮伺服器已中斷連線並觸發 onclose 事件。
        //.withKeepAliveInterval(30000)
        .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
        .build();
   

    connection.on("ReceivePrivateMessage", function (sender, message) {
        addMessage(`${sender}: ${message}`);
    });

    connection.on("ReceiveGroupMessage", (chatroom, sender, message) => {
        addMessage(`[${chatroom}] ${sender}: ${message}`);
    });

    connection.on("ContainerChanged", (containerInfo) => {
        getContainerInfo(containerInfo)
    });

    connection.onreconnecting(err => {
        console.log("正在重連...", err);
    });
    connection.onreconnected(() => {
        console.log("重連成功");
    });
    connection.onclose(err => {
        // 嘗試刷新 Token 後重新連線
        fetch(`${apiUrl}/api/auth/refresh`, {
            method: 'POST',
            credentials: 'include'
        }).then(refreshRes => {
            if (!refreshRes.ok) {
                return Promise.reject("刷新 token 失敗")
            }
            return refreshRes.json();
        })
        .then(data => {
            document.cookie = `jwt=${data.token}; path=/`;
            start();
        })
        .catch(err => {
            console.error(err);
            logout();
        });
    });


    // todo 斷線測試重連
    function start() {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            connection.start()
                .then(() => console.log("連線成功"))
                .catch(err => {
                    console.error("連線失敗:", err);
                    /*                    
                      Error: WebSocket failed to connect. The connection could not be found on the server, either the endpoint may not be a SignalR endpoint, the connection ID is not present on the server, or there is a proxy blocking WebSockets. 
                      If you have multiple servers check that sticky sessions are enabled.
                    */
                    // 是否為 401 過期
                    // 嘗試刷新 Token 後重新連線
                    fetch(`${apiUrl}/api/auth/refresh`, {
                        method: 'POST',
                        credentials: 'include'
                    }).then(refreshRes => {
                        if (!refreshRes.ok) {
                            return Promise.reject("刷新 token 失敗")
                        }
                        return refreshRes.json();
                    })
                    .then(data => {
                        document.cookie = `jwt=${data.token}; path=/`;
                        start();
                    })
                    .catch(err => {
                        console.error(err);
                        logout();
                    });


                });
        }
    }
    start()
}

// 獲取 Cookie
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            atob(base64).split('').map(function (c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join('')
        );
        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

function callProtectedApi() {
    fetchWithAuth(`${apiUrl}/api/auth/test`, {
        method: 'GET',
    }).then(res => {
        if (!res.ok) {
            return res.json().then(err => {
                return Promise.reject(err);
            });
        }
        return res.text()
    })
    .then(data => console.log(data))
    .catch(err => {
        console.error(err.message)
        logout()
    });
}

window.onload = () => {
    const token = getCookie("jwt");
    if (token) {
        const payload = parseJwt(token);
        const username = payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"]
        const userId = payload?.["UserId"];

        if (username && userId) {
            showLoggedInState(username, userId);
            connectToSignalR();
            return;
        }
    }

    const refreshToken = getCookie("refresh");
    if (refreshToken) {

        // todo 提交 refresh token
        fetch(`${apiUrl}/api/auth/refresh`, {
            method: 'POST',
            credentials: 'include'
        })
        .then(res => {
            if (!res.ok) return Promise.reject("refresh token 刷新失敗");
            return res.json();
        })
        .then(data => {
            // 只會回傳 jwt
            document.cookie = `jwt=${data.token}; path=/`;
            const payload = parseJwt(data.token);
            const username = payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"];
            const userId = payload?.["UserId"];
            if (username && userId) {
                showLoggedInState(username, userId);
                connectToSignalR();
                return;
            }
            showLoggedOutState();
        })
        .catch((err) => {
            console.log(err);
            showLoggedOutState();
        });
        return;
    }
    showLoggedOutState();
};