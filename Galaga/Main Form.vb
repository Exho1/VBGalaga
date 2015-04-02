Public Class frmMain

    ' Rewrite collision detection 
    ' Finish dropping enemies
    ' Find boss texture

    Dim gameRunning As Boolean = False
    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Play our starting sound
        My.Computer.Audio.Play(My.Resources.galaga_levelstart, AudioPlayMode.Background)

        startDelay.Start()
    End Sub

    Private Sub startDelay_Tick(sender As Object, e As EventArgs) Handles startDelay.Tick
        ' Timer has ticked, start the game
        gameStart()
        startDelay.Stop()
    End Sub

    Dim enemies = New List(Of enemy)
    Dim bullets = New List(Of playerBullet)
    Dim score As Integer = 0
    Private Sub gameStart()
        gameRunning = True

        ' Reset the bullets and enemies
        ReDim bullets(1)
        ReDim enemies(1)
        score = 0

        ' Add enemies
        Dim createdEnemies = 0
        Dim xBuffer = 70
        Dim yBuffer = 60
        Dim type = "red"
        Dim row As Integer = 1
        For index As Integer = 1 To 40
            Dim pos = New Point(xBuffer, yBuffer)

            ' Create a new enemy
            Dim newEnemy = New enemy(player, pos, type)
            Me.Controls.Add(newEnemy)

            ' Expand the array
            ReDim Preserve enemies(enemies.Length + 1)

            ' Add the new enemy
            enemies(createdEnemies) = newEnemy

            ' Class variables
            newEnemy.row = row
            newEnemy.originalLocation = newEnemy.Location
            newEnemy.type = type
            newEnemy.id = index

            ' Every other row moves the same direction
            If newEnemy.row Mod 2 = 0 Then
                newEnemy.moveLeft = Not newEnemy.moveLeft
            End If

            createdEnemies += 1
            xBuffer += newEnemy.Size.Width + 30

            ' Every 10 enemies, create a new row
            If index Mod 10 = 0 Then
                row += 1

                ' Every 2 rows, switch color
                If index Mod 20 = 0 Then
                    If type = "red" Then
                        type = "blue"
                    Else
                        type = "red"
                    End If
                End If

                yBuffer += newEnemy.Size.Height + 20
                xBuffer = 70
            End If
        Next
    End Sub

    Dim playerVelocity As Double = 0
    Dim time As Double = 0
    Dim nextMove As Double = 0
    Dim nextDrop As Double = 0
    Dim nextGC As Integer = 0
    Private Sub gameUpdate()
        ' Update the player location and velocity
        playerVelocity = playerVelocity * 0.9
        Dim pX = player.Location.X + playerVelocity

        ' Move the player
        player.Location = New Point(pX, player.Location.Y)

        ' Clamp the player's location
        If player.Location.X <= 0 Then
            player.Location = New Point(0, player.Location.Y)
        ElseIf player.Location.X + player.Size.Width >= Me.Width Then
            player.Location = New Point(Me.Width - player.Size.Width, player.Location.Y)
        End If

        ' Check if bullets have hit something
        For Each bullet As playerBullet In bullets
            If Not IsNothing(bullet) Then
                ' debug.Text = bullet.Location.Y
                If bullet.Location.Y + bullet.Size.Height <= 0 Then
                    bullet = Nothing
                Else
                    bullet.Location = New Point(bullet.Location.X, bullet.Location.Y - 10)
                End If

                For Each enemy As enemy In enemies
                    If Not IsNothing(enemy) And Not IsNothing(bullet) Then
                        If Not enemy.dead And Not bullet.dead Then
                            If bullet.Location.X >= enemy.Location.X And bullet.Location.X + bullet.Size.Width <= enemy.Location.X + enemy.Size.Width Then
                                If bullet.Location.Y <= enemy.Location.Y + enemy.Size.Height And bullet.Location.Y >= enemy.Location.Y Then
                                    enemy.dead = True
                                    bullet.dead = True

                                    If enemy.type.ToLower() = "red" Then
                                        score += 80
                                    Else
                                        score += 50
                                    End If

                                    txtScore.Text = score

                                    My.Computer.Audio.Play(My.Resources.galaga_death1, AudioPlayMode.Background)

                                    Me.Controls.Remove(enemy)
                                    enemy.Dispose()

                                    Me.Controls.Remove(bullet)
                                    bullet.Dispose()
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Next

        For Each enemy As enemy In enemies
            If Not IsNothing(enemy) Then
                If enemy.Location.X >= player.Location.X And enemy.Location.X + enemy.Size.Width <= player.Location.X + player.Size.Width Then
                    If enemy.Location.Y <= player.Location.Y + player.Size.Height And enemy.Location.Y >= player.Location.Y Then
                        enemy.dead = True

                        Me.Controls.Remove(enemy)
                        enemy.Dispose()

                        Me.Controls.Remove(player)
                        player.Dispose()

                        lblGameOver.Visible = True

                        gameRunning = False
                    End If
                End If
            End If
        Next

        ' Move the enemies
        If time > nextMove Then
            For Each enemy As enemy In enemies
                If Not IsNothing(enemy) Then
                    If Not enemy.dead And Not enemy.dropping Then
                        Dim x As Integer = enemy.Location.X
                        Dim y As Integer = enemy.Location.Y

                        If enemy.moveLeft = True Then
                            enemy.Location = New Point(enemy.Location.X - 5, enemy.Location.Y)
                        Else
                            enemy.Location = New Point(enemy.Location.X + 5, enemy.Location.Y)
                        End If

                        ' Clamp how far left or right they can go
                        If enemy.Location.X - enemy.originalLocation.X > 20 Or enemy.originalLocation.X - enemy.Location.X > 20 Then
                            enemy.moveLeft = Not enemy.moveLeft
                        End If
                    ElseIf enemy.dropping Then
                        Dim x = enemy.Location.X + (Math.Cos(enemy.Location.Y / 200) * 50)
                        Dim y = enemy.Location.Y + 10

                        enemy.Location = New Point(x, y)

                        If enemy.Location.Y > Me.Height Then
                            enemy.dead = True
                            enemy.dropping = False
                        End If
                    End If
                End If
            Next
            nextMove = time + 1
        End If

        If time > nextDrop Then
            For i As Integer = 0 To 40 Step 10
                If i <> 0 And Not IsNothing(enemies(i)) Then
                    Dim enemy As enemy = enemies(i)

                    If Not enemy.dead Then
                        enemy.dropping = True
                        Exit For
                    End If
                End If
            Next

            nextDrop = time + 5
        End If

        ' Garbage collection - Remove all 'dead' entities from the list
        If time > nextGC Then
            Dim newEnemies = New List(Of enemy)
            Dim newBullets = New List(Of playerBullet)

            ' Enemies 
            For Each enemy As enemy In enemies
                If Not IsNothing(enemy) Then
                    If Not enemy.dead Then
                        newEnemies.Add(enemy)
                    End If
                End If
            Next

            enemies = newEnemies

            ' Bullets 
            For Each bullet As playerBullet In bullets
                If Not IsNothing(bullet) Then
                    If Not bullet.dead Then
                        newBullets.Add(bullet)
                    End If
                End If
            Next

            bullets = newBullets

            nextGC = time + 5
        End If
    End Sub

    ' Detect key pressing
    Private Sub frmMain_KeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles Me.KeyPress
        If e.KeyChar.ToString() = "a" Then
            playerVelocity = -10
        ElseIf e.KeyChar.ToString() = "d" Then
            playerVelocity = 10
        End If
    End Sub

    Dim createdShots = 0
    Private Sub shootBullet()
        If gameRunning = False Then
            Return
        End If

        ' Create a new bullet
        Dim newShot = New playerBullet(player)
        Me.Controls.Add(newShot)

        ' Expand the array
        'ReDim Preserve bullets(bullets.Length + 1)

        ' Add the new bullet
        bullets.Add(newShot)
        'bullets(createdShots) = newShot

        My.Computer.Audio.Play(My.Resources.galaga_shooting, AudioPlayMode.Background)

        createdShots += 1
    End Sub

    Private Sub background_Click(sender As Object, e As EventArgs) Handles Me.Click
        shootBullet()
    End Sub

    Private Sub updateTimer_Tick(sender As Object, e As EventArgs) Handles updateTimer.Tick
        time += 0.1

        If gameRunning = True Then
            gameUpdate()
        End If
    End Sub
End Class

' The player's bullet class
Public Class playerBullet
    Inherits PictureBox

    Public dead As Boolean = False

    ' Constructor
    Public Sub New(ByVal player As PictureBox)
        Image = My.Resources.galaga_bullet
        SizeMode = PictureBoxSizeMode.StretchImage
        Size = New Size(17, 35)
        BackColor = Color.Black
        Location = New Point(player.Location.X + player.Size.Width / 2 - Size.Width / 2, player.Location.Y)
    End Sub

End Class

' Enemy class
Public Class enemy
    Inherits PictureBox

    Public type As String
    Public row As Integer
    Public id As Integer
    Public dead As Boolean = False
    Public dropping As Boolean = False

    Public originalLocation As Point
    Public moveLeft As Boolean = False

    ' Constructor
    Public Sub New(ByVal player As PictureBox, ByVal pos As Point, ByVal type As String)
        If type.ToLower() = "red" Then
            Image = My.Resources.galaga_enemy1
        Else
            Image = My.Resources.galaga_enemy2
        End If
        SizeMode = PictureBoxSizeMode.StretchImage
        Size = New Size(40, 40)
        BackColor = Color.Black
        Location = New Point(pos)
    End Sub

End Class