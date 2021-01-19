﻿using Assets.Game.Scripts.Core.Common;
using Assets.Game.Scripts.Domain.Contexts;
using Assets.Game.Scripts.Domain.Systems;
using Assets.Game.Scripts.Domain.Views;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Assets.Game.Scripts.Domain.Components
{
    public class Protagonist : MonoBehaviour, IContextObserver
    {
#pragma warning disable 0649
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _armsRoot;
#pragma warning restore 0649

        private GameInputSystem _inputSystem;
        private SettingsSystem _settings;
        private WeaponSystem _weaponSystem;

        private List<WeaponView> _weaponViews;
        private WeaponView _currentView;

        private Vector2 _previousMovementInput;
        private Vector2 _lookDelta;
        private Vector3 _move, _velocity;
        private float _pitchRotation, _yawRotation;

        [Inject]
        public void Construct(GameInputSystem inputSystem, SettingsSystem settingsSystem, WeaponSystem weaponSystem)
        {
            _inputSystem = inputSystem;
            _weaponSystem = weaponSystem;
            _settings = settingsSystem;
        }

        private void OnEnable()
        {
            SetupWeapons();

            _inputSystem.MoveEvent += OnMove;
            _inputSystem.LookEvent += OnLook;
            _inputSystem.JumpEvent += OnJump;
            _inputSystem.FireEvent += OnFire;
            _inputSystem.SwitchWeapon += OnSwitchWeapon;
        }

        private void OnDisable()
		{
			_inputSystem.MoveEvent -= OnMove;
            _inputSystem.LookEvent -= OnLook;
            _inputSystem.JumpEvent -= OnJump;
            _inputSystem.FireEvent -= OnFire;
            _inputSystem.SwitchWeapon -= OnSwitchWeapon;
        }

        private void SetupWeapons()
        {
            _weaponViews = new List<WeaponView>(GameContext.Current.Weapons.Count);

            foreach (var weapon in GameContext.Current.Weapons)
            {
                var view = Instantiate(weapon.Model.ViewPrefab, _armsRoot);
                
                view.Hide();
                view.Attach(weapon);
                
                _weaponViews.Add(view);
            }
        }

        private void Update()
        {
            UpdateRotation();

            UpdateMovement();

            UpdateVelocity();
        }

        private void UpdateRotation()
        {
            _pitchRotation -= _lookDelta.y * _settings.MouseSensitivity * Time.deltaTime;
            _pitchRotation = Mathf.Clamp(_pitchRotation, -90f, 90f);
            _armsRoot.localRotation = Quaternion.Euler(_pitchRotation, 0, 0);

            _yawRotation += _lookDelta.x * _settings.MouseSensitivity * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0, _yawRotation, 0);
        }

        private void UpdateMovement()
        {
            _move = _armsRoot.right * _previousMovementInput.x + _armsRoot.forward * _previousMovementInput.y;
            _move.y = 0;

            _characterController.Move(Time.deltaTime * _settings.MovementSpeed * _move);
        }

        private void UpdateVelocity()
        {
            _characterController.Move(_velocity * Time.deltaTime);

            if (!_characterController.isGrounded)
            {
                _velocity.y += _settings.Gravity * Time.deltaTime;
            }
        }

        private void OnMove(Vector2 direction)
        {
            _previousMovementInput = direction;
        }
        private void OnLook(Vector2 delta)
        {
            _lookDelta = delta;
        }
        private void OnJump()
        {
            if (_characterController.isGrounded)
            {
                _velocity.y = _settings.JumpHeight;
            }
        }
        private void OnFire()
        {
            if (_weaponSystem.CanFire())
            {
                var ray = _currentView.GetFireRay();
                _weaponSystem.OnFire(ray);
            }
        }
        private void OnSwitchWeapon(bool next)
        {
            _weaponSystem.SwitchWeapon(next);
        }

        private void UpdateShownWeapon()
        {
            if (_currentView != null)
            {
                _currentView.Hide();
            }

            _currentView = _weaponViews.Find(x => x.Context == GameContext.Current.SelectedWeapon);
            _currentView.Show();
        }

        #region Observer
        public void Refresh(int propertyId)
        {
            switch (propertyId)
            {
                case GameContext.WeaponChangedEvent:
                    UpdateShownWeapon();
                    break;

                default:
                    break;
            }
        }

        private IContext _context;
        public void Attach(IContext context) 
        {
            context.Attach(this);
            _context = context;
        }
        public void Detach() 
        {
            _context?.Detach(this);
        }
        #endregion
    }
}
