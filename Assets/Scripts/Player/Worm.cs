using System;
using System.Collections;
using System.Collections.Generic;
using KawaiiImplementation;
using Mirror;
using UnityEngine;

public class Worm : NetworkBehaviour
{
    #region Visual
    public Color playColor = Color.red;
    private readonly Color originalColor = Color.white;
    [SyncVar(hook = nameof(HandleColorChanged))]
    private Color color;
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SyncVar(hook = nameof(AssignSlimeType))]
    public KawaiiSlimeSelector.KawaiiSlime slimeType;
    [SerializeField]
    private List<GameObject> slimes;
    
    #endregion

    #region Slime's Components

    [SerializeField]
    private Transform slimeHolder;
    public Transform SlimeHolder => slimeHolder;
    // [SyncVar(hook = nameof(HandleChangeSlime))]
    public GameObject slime;

    #endregion

    private GameManager gameManager;
    private GameManager GameManager
    {
        get
        {
            if (gameManager != null) return gameManager;
            return gameManager = GameManager.instance;
        }
    }
    
    #region Play
    private bool isTurnActive = false;
    public bool IsTurnActive 
    { get => isTurnActive; private set => isTurnActive = value; }
    private Controls controls;
    public Controls Controls
    {
        get
        {
            if (controls != null) return controls;
            return controls = new Controls();
        }
    }

    [SerializeField]
    private PlayerController controller;
    public PlayerController Controller => controller;
    [SerializeField]
    private PlayerWeapon weapon;
    public PlayerWeapon Weapon => weapon;

    [SerializeField]
    private PlayerCanvas canvas;
    public PlayerCanvas Canvas => canvas;

    private PlayerCamera camera;
    public PlayerCamera Camera => camera;

    public event Action OnTurnStarted;
    public event Action OnTurnEnded;

    #endregion

    
    #region Server

    /// <summary>
    /// Start the turn of the player client-wise
    /// </summary>
    [Server]
    public void StartTurn(int time)
    {
        IsTurnActive = true;
        color = playColor;
        TargetToggleControls(this.connectionToClient, true);
        OnTurnStarted?.Invoke();
        canvas.TargetEnableTurnHUD(this.connectionToClient, time);
    }
    

    /// <summary>
    /// End turn of the player client-wise
    /// </summary>
    [Server]
    public void EndTurn()
    {
        IsTurnActive = false;
        color = originalColor;
        TargetToggleControls(this.connectionToClient, false);
        OnTurnEnded?.Invoke();
        canvas.TargetDisableTurnHUD(this.connectionToClient);
    }


    [Server]
    public void CreateMySlime(GameObject theSlime)
    {
        slime = Instantiate(theSlime, slimeHolder);
    }
    
    #endregion

    
    #region Client

    /// <summary>
    /// Everytime the color is changed on the server, it changes the color on all the clients
    /// </summary>
    /// <param name="oldColor">Old Color</param>
    /// <param name="newColor">New Color</param>
    [Client]
    private void HandleColorChanged(Color oldColor, Color newColor)
    {
        meshRenderer.material.color = newColor;
    }


    /// <summary>
    /// Enable or disable the controls of the targeted Client
    /// </summary>
    /// <param name="conn">Client targeted</param>
    /// <param name="toggle">Enable or disable Controls</param>
    [TargetRpc]
    private void TargetToggleControls(NetworkConnection conn, bool toggle)
    {
        if (toggle)
            Controls.Enable();
        else
            Controls.Disable();
    }
    
    
    [Client]
    public void SetSlime(GameObject slimeObject)
    {
        slime = Instantiate(slimeObject, slimeHolder);
        NetworkServer.Spawn(slime);
        Controller.slimeAnimator = slime.GetComponent<AnimateSlime>();
    }
    
    
    [ClientRpc]
    public void RpcSetSlime(GameObject slimeObject, KawaiiSlimeSelector.KawaiiSlime type)
    {
        if (slimeObject != null)
        {
            Debug.Log($"<color=green>slime object != null !</color>");
            slime = Instantiate(slimeObject, slimeHolder);
        } else
        {
            GameTestNetworkManager manager = NetworkManager.singleton as GameTestNetworkManager;
            slime = Instantiate(manager.slimeSelector.SelectSlime(type), slimeHolder);
            
        }
        NetworkServer.Spawn(slime);
        Controller.slimeAnimator = slime.GetComponent<AnimateSlime>();
    }


    [TargetRpc]
    public void TargetSetSlime(NetworkConnection conn, GameObject slimeObject)
    {
        slime = Instantiate(slimeObject, slimeHolder);
        NetworkServer.Spawn(slime);
        Controller.slimeAnimator = slime.GetComponent<AnimateSlime>();
    }



    [Client]
    private void HandleChangeSlime(GameObject oldSlime, GameObject newSlime)
    {
        Debug.Log($"<color=blue>Update Slime</color>");
        slime = newSlime;
    }


    [Client]
    private void AssignSlimeType(KawaiiSlimeSelector.KawaiiSlime oldType, KawaiiSlimeSelector.KawaiiSlime newType)
    {
        slime = slimes[(int)newType];
        
        slimes[(int)oldType].SetActive(false);
        slimes[(int)newType].SetActive(true);
    }
    
    #endregion
}