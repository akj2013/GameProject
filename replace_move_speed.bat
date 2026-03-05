@echo off
cd /d "%~dp0"
cd Assets\Resources\UI
if not exist UpgradePanel_MoveSpeed_fixed.png (
    echo UpgradePanel_MoveSpeed_fixed.png not found.
    pause
    exit /b 1
)
del UpgradePanel_MoveSpeed.png
ren UpgradePanel_MoveSpeed_fixed.png UpgradePanel_MoveSpeed.png
echo Done. You can reopen Unity.
pause
