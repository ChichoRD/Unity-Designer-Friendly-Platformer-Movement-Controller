﻿using ChichoExtensions;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct Dasher2D
{
    public UnityEvent OnDash;

    [SerializeField][Min(0)] private float dashImpulse; //30f
    [SerializeField][Min(0)] private float dashSkipTime; //0.4f
    [SerializeField][Min(0)] private LayerMask dashSkipLayers;
    [SerializeField][Min(0)] private float _coolDownTime;

    private readonly Rigidbody2D _rigidbody;
    private Task dashSkipTask;
    private Task _coolDownTask;

    public float DashForce { get => dashImpulse; }
    public LayerMask DashSkipLayers { get => dashSkipLayers; }

    public Dasher2D(Dasher2D oldDasher, Rigidbody2D rigidbody) : this()
    {
        OnDash = oldDasher.OnDash;
        dashImpulse = oldDasher.dashImpulse;
        dashSkipTime = oldDasher.dashSkipTime;
        dashSkipLayers = oldDasher.dashSkipLayers;
        dashSkipTask = oldDasher.dashSkipTask;
        _coolDownTime = oldDasher._coolDownTime;

        _rigidbody = rigidbody;
    }

    public void Dash(Vector2 direction)
    {
        if (_rigidbody == null || (_coolDownTask != null && !_coolDownTask.IsCompleted)) return;

        Vector2 impulse = direction.normalized * dashImpulse;
        if (impulse.sqrMagnitude > 0)
            OnDash?.Invoke();

        _rigidbody.AddForce(impulse, ForceMode2D.Impulse);
        _coolDownTask = CooldownTask();

        if (dashSkipTask?.Status == TaskStatus.Running) return;
        dashSkipTask = SkipLayerCollision();
    }

    private async Task SkipLayerCollision()
    {
        const int MILISECONDS = 1000;
        int objectLayer = _rigidbody.gameObject.layer;
        int skipLayer = dashSkipLayers.FirstSetLayer();

        Physics2D.IgnoreLayerCollision(objectLayer, skipLayer, true);
        await Task.Delay((int)(dashSkipTime * MILISECONDS));
        Physics2D.IgnoreLayerCollision(objectLayer, skipLayer, false);
    }

    private async Task CooldownTask()
    {
        const int MILISECONDS = 1000;
        await Task.Delay((int)(_coolDownTime * MILISECONDS));
        _coolDownTask.Dispose();
    }
}
