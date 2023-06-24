using RiptideNetworking;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();
    public Player player;

    public ushort Id { get; private set; }
    public string Name { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string Password { get; private set; }

    private void Start()
    {
        player = this;
    }

    private void OnDestroy()
    {
        list.Remove(Id);
    }
    
    public static void Spawn(ushort id)
    {
        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ";
        player.Id = id;
        player.SendSpawned(id);
        list.Add(id, player);
    }

    public void SignUp(ushort id, string name, string username, string email,  string password)
    {
        Debug.Log("2");
        StartCoroutine(CheckSignUp(id, name, username, email,  password));
    }

    public void Login(ushort id, string email, string password)
    { 
        StartCoroutine(CheckLogin(id, email, password));
    }
    public void sendChat(string chat )
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.sendChat);
        message.AddString(chat);
        NetworkManager.Singleton.Server.Send(message, Id);
    } 

    #region Messages
    /*private void SendSpawned()
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.playerSignUp)));
    }*/

    private void SendSpawned(ushort toClientId)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.connect)), toClientId);
    }

    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        return message;
    }

    private static void SendSignUpResult(ushort fromClientId, string result)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.signUpResult);
        message.AddString(result);
        NetworkManager.Singleton.Server.Send(message, fromClientId);
    }

    private void SendLoginResult(ushort fromClientId)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.loginResult);
        message.AddString(Name);
        message.AddString(Username);
        message.AddString(Email);
        NetworkManager.Singleton.Server.Send(message, fromClientId);
    }

    private void LoginError(ushort fromClientId, string index)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.loginError);
        message.AddString(index);
        NetworkManager.Singleton.Server.Send(message, fromClientId);
    }

    [MessageHandler((ushort)ClientToServerId.connect)]
    private static void Connect(ushort fromClientId, Message message)
    {
        Spawn(fromClientId);
    }

    [MessageHandler((ushort)ClientToServerId.login)]
    private static void Login(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
            player.Login(fromClientId, message.GetString(), message.GetString());
    }

    [MessageHandler((ushort)ClientToServerId.signUp)]
    private static void SignUp(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
            player.SignUp(fromClientId, message.GetString(), message.GetString(), message.GetString(), message.GetString());
    }

    [MessageHandler((ushort)ClientToServerId.sendChat)]
    private static void sendChat(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
            player.sendChat(message.GetString());
    }

    

    #endregion

    IEnumerator CheckSignUp(ushort id, string _name, string _username, string _email,  string _password)
    {
        WWWForm form = new WWWForm();
        form.AddField("name", _name);
        form.AddField("username", _username);
        form.AddField("email", _email);
        form.AddField("password", _password);
        WWW www = new WWW("http://localhost/Joinone/register.php", form);
        yield return www;
        SendSignUpResult(id, www.text);
        if (www.text == "0"){
            Debug.Log("User created successfully.");
        }else{
            Debug.Log("User Creation failed. Error #" + www.text);
        }
        www.Dispose();
    }

    IEnumerator CheckLogin(ushort id, string _email, string _password)
    {
        Debug.Log("18");
        WWWForm form = new WWWForm();
        form.AddField("email", _email);
        form.AddField("password", _password);
        WWW www = new WWW("http://localhost/Joinone/login.php", form);
        yield return www;
        if (www.text[0] == '0'){
            StartCoroutine(WaitForDataUpdate(www));   
            Debug.Log("19");  
        }else{
            LoginError(id, www.text);
            Debug.Log("User login failed. Error #" + www.text);
        }
        www.Dispose();
    }

    IEnumerator WaitForDataUpdate(WWW www)
    {
        Name = www.text.Split('\t')[1];
        Username = www.text.Split('\t')[2];
        Email = www.text.Split('\t')[3];

        SendLoginResult(Id);
        Debug.Log("User login successfully");
        yield return null;
    }
}
