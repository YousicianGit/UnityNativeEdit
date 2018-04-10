#import "EditBox_iOS.h"
#import "PlaceholderTextView.h"
#import <MobileCoreServices/UTCoreTypes.h>

/// UnityEditBox Plugin
/// Written by bkmin 2014/11 Nureka Inc.

UIViewController* unityViewController = nil;
NSMutableDictionary*    dictEditBox = nil;
EditBoxHoldView*         viewPlugin = nil;

char    g_unityName[64];
int characterLimit;

bool approxEqualFloat(float x, float y)
{
    return fabs(x-y) < 0.001f;
}

@implementation EditBoxHoldView

-(id) initHoldView:(CGRect) frame
{
    if (self = [super initWithFrame:frame])
    {
        UITapGestureRecognizer *tap=[[UITapGestureRecognizer alloc] initWithTarget:self    action:@selector(tapAction:)];
        tap.cancelsTouchesInView = NO;
        [self addGestureRecognizer:tap];
        self.userInteractionEnabled = YES;
    }
    return self;
}

-(void) tapAction:(id) sender
{
    for (EditBox *eb in [dictEditBox allValues])
    {
        if ([eb IsFocused])
        {
            [eb showKeyboard:NO];
        }
    }
}

@end


@implementation EditBox

+(void) initializeEditBox:(UIViewController*) _unityViewController  unityName:(const char*) unityName
{
    unityViewController = _unityViewController;
    dictEditBox = [[NSMutableDictionary alloc] init];
    
    CGRect frameView = unityViewController.view.frame;
    frameView.origin = CGPointMake(0.0f, 0.0f);
    viewPlugin = [[EditBoxHoldView alloc] initHoldView:frameView];
    [unityViewController.view addSubview:viewPlugin];
    
    strcpy(g_unityName, unityName);
}

+(JsonObject*) makeJsonRet:(BOOL) isError error:(NSString*) strError
{
    JsonObject* jsonRet = [[JsonObject alloc] init];
    
    [jsonRet setBool:@"bError" value:isError];
    [jsonRet setString:@"strError" value:strError];
    return jsonRet;
}

+(JsonObject*) processRecvJsonMsg:(int)nSenderId msg:(JsonObject*) jsonMsg
{
    JsonObject* jsonRet;
    
    NSString* msg = [jsonMsg getString:@"msg"];
    if ([msg isEqualToString:MSG_CREATE])
    {
        EditBox* nb = [[EditBox alloc] initWithViewController:unityViewController _tag:nSenderId];
        [nb create:jsonMsg];
        [dictEditBox setObject:nb forKey:[NSNumber numberWithInt:nSenderId]];
        jsonRet = [EditBox makeJsonRet:NO error:@""];
    }
    else
    {
        EditBox* b = [dictEditBox objectForKey:[NSNumber numberWithInt:nSenderId]];
        if (b)
        {
            jsonRet = [b processJsonMsg:msg json:jsonMsg];
        }
        else
        {
            jsonRet = [EditBox makeJsonRet:YES error:@"EditBox not found"];
        }
    }
    return jsonRet;
}

+(void) finalizeEditBox
{
    NSArray* objs = [dictEditBox allValues];
    for (EditBox* b in objs)
    {
        [b remove];
    }
    dictEditBox = nil;
}

-(BOOL) IsFocused
{
    return editView.isFirstResponder;
}

-(void) sendJsonToUnity:(JsonObject*) json
{
    [json setInt:@"senderId" value:tag];
    
    
    NSData *jsonData = [json serialize];
    NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    UnitySendMessage(g_unityName, "OnMsgFromPlugin", [jsonString UTF8String]);
}

-(JsonObject*) processJsonMsg:(NSString*) msg json:(JsonObject*) jsonMsg
{
    JsonObject* jsonRet = [EditBox makeJsonRet:NO error:@""];
    if ([msg isEqualToString:MSG_REMOVE])
    {
        [self remove];
    }
    else if ([msg isEqualToString:MSG_SET_TEXT])
    {
        [self setText:jsonMsg];
    }
    else if ([msg isEqualToString:MSG_GET_TEXT])
    {
        NSString* text = [self getText];
        [jsonRet setString:@"text" value:text];
    }
    else if ([msg isEqualToString:MSG_SET_RECT])
    {
        [self setRect:jsonMsg];
    }
    else if ([msg isEqualToString:MSG_SET_FOCUS])
    {
        BOOL isFocus = [jsonMsg getBool:@"isFocus"];
        [self setFocus:isFocus];
    }
    else if ([msg isEqualToString:MSG_SET_VISIBLE])
    {
        BOOL isVisible = [jsonMsg getBool:@"isVisible"];
        [self setVisible:isVisible];
    }
    
    return jsonRet;
}

-(id)initWithViewController:(UIViewController*)theViewController _tag:(int)_tag
{
    if(self = [super init]) {
        viewController = theViewController;
        tag = _tag;
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboardWillShow:) name:UIKeyboardWillShowNotification object:nil];
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboardWillHide:) name:UIKeyboardWillHideNotification object:nil];
    }
    return self;
}

-(void) setRect:(JsonObject*)json
{
    float x = [json getFloat:@"x"] * viewController.view.bounds.size.width;
    float y = [json getFloat:@"y"] * viewController.view.bounds.size.height;
    float width = [json getFloat:@"width"] * viewController.view.bounds.size.width;
    float height = [json getFloat:@"height"] * viewController.view.bounds.size.height;
    
    x -= editView.superview.frame.origin.x;
    y -= editView.superview.frame.origin.y;
    editView.frame = CGRectMake(x, y, width, height);
}

-(void) create:(JsonObject*)json
{
    NSString* placeholder = [json getString:@"placeHolder"];
    
    NSString* font = [json getString:@"font"];
    float fontSize = [json getFloat:@"fontSize"];
    
    float x = [json getFloat:@"x"] * viewController.view.bounds.size.width;
    float y = [json getFloat:@"y"] * viewController.view.bounds.size.height;
    float width = [json getFloat:@"width"] * viewController.view.bounds.size.width;
    float height = [json getFloat:@"height"] * viewController.view.bounds.size.height;
    
    characterLimit = [json getInt:@"characterLimit"];
    
    float textColor_r = [json getFloat:@"textColor_r"];
    float textColor_g = [json getFloat:@"textColor_g"];
    float textColor_b = [json getFloat:@"textColor_b"];
    float textColor_a = [json getFloat:@"textColor_a"];
    UIColor* textColor = [UIColor colorWithRed:textColor_r green:textColor_g blue:textColor_b alpha:textColor_a];
    
    float backColor_r = [json getFloat:@"backColor_r"];
    float backColor_g = [json getFloat:@"backColor_g"];
    float backColor_b = [json getFloat:@"backColor_b"];
    float backColor_a = [json getFloat:@"backColor_a"];
    UIColor* backgroundColor = [UIColor colorWithRed:backColor_r green:backColor_g blue:backColor_b alpha:backColor_a];
    
    float placeHolderColor_r = [json getFloat:@"placeHolderColor_r"];
    float placeHolderColor_g = [json getFloat:@"placeHolderColor_g"];
    float placeHolderColor_b = [json getFloat:@"placeHolderColor_b"];
    float placeHolderColor_a = [json getFloat:@"placeHolderColor_a"];
    UIColor* placeHolderColor = [UIColor colorWithRed:placeHolderColor_r green:placeHolderColor_g blue:placeHolderColor_b alpha:placeHolderColor_a];
    
    NSString* contentType = [json getString:@"contentType"];
    NSString* alignment = [json getString:@"align"];
    BOOL withDoneButton = [json getBool:@"withDoneButton"];
    BOOL multiline = [json getBool:@"multiline"];
    
    BOOL autoCorr = NO;
    BOOL password = NO;
    UIKeyboardType keyType = UIKeyboardTypeDefault;
    
    if ([contentType isEqualToString:@"Autocorrected"])
    {
        autoCorr = YES;
    }
    else if ([contentType isEqualToString:@"IntegerNumber"])
    {
        keyType = UIKeyboardTypeNumberPad;
    }
    else if ([contentType isEqualToString:@"DecimalNumber"])
    {
        keyType = UIKeyboardTypeDecimalPad;
    }
    else if ([contentType isEqualToString:@"Alphanumeric"])
    {
        keyType = UIKeyboardTypeAlphabet;
    }
    else if ([contentType isEqualToString:@"Name"])
    {
        keyType = UIKeyboardTypeNamePhonePad;
    }
    else if ([contentType isEqualToString:@"EmailAddress"])
    {
        keyType = UIKeyboardTypeEmailAddress;
    }
    else if ([contentType isEqualToString:@"Password"])
    {
        password = YES;
    }
    else if ([contentType isEqualToString:@"Pin"])
    {
        keyType = UIKeyboardTypePhonePad;
    }
    
    UIControlContentHorizontalAlignment halign = UIControlContentHorizontalAlignmentLeft;
    UIControlContentVerticalAlignment valign = UIControlContentVerticalAlignmentCenter;
    
    if ([alignment isEqualToString:@"UpperLeft"])
    {
        valign = UIControlContentVerticalAlignmentTop;
        halign = UIControlContentHorizontalAlignmentLeft;
    }
    else if ([alignment isEqualToString:@"UpperCenter"])
    {
        valign = UIControlContentVerticalAlignmentTop;
        halign = UIControlContentHorizontalAlignmentCenter;
    }
    else if ([alignment isEqualToString:@"UpperRight"])
    {
        valign = UIControlContentVerticalAlignmentTop;
        halign = UIControlContentHorizontalAlignmentRight;
    }
    else if ([alignment isEqualToString:@"MiddleLeft"])
    {
        valign = UIControlContentVerticalAlignmentCenter;
        halign = UIControlContentHorizontalAlignmentLeft;
    }
    else if ([alignment isEqualToString:@"MiddleCenter"])
    {
        valign = UIControlContentVerticalAlignmentCenter;
        halign = UIControlContentHorizontalAlignmentCenter;
    }
    else if ([alignment isEqualToString:@"MiddleRight"])
    {
        valign = UIControlContentVerticalAlignmentCenter;
        halign = UIControlContentHorizontalAlignmentRight;
    }
    else if ([alignment isEqualToString:@"LowerLeft"])
    {
        valign = UIControlContentVerticalAlignmentBottom;
        halign = UIControlContentHorizontalAlignmentLeft;
    }
    else if ([alignment isEqualToString:@"LowerCenter"])
    {
        valign = UIControlContentVerticalAlignmentBottom;
        halign = UIControlContentHorizontalAlignmentCenter;
    }
    else if ([alignment isEqualToString:@"LowerRight"])
    {
        valign = UIControlContentVerticalAlignmentBottom;
        halign = UIControlContentHorizontalAlignmentRight;
    }
   
    if (withDoneButton)
    {
        keyboardDoneButtonView  = [[UIToolbar alloc] init];
        [keyboardDoneButtonView sizeToFit];
        doneButton = [[UIBarButtonItem alloc] initWithTitle:@"Done" style:UIBarButtonItemStyleDone target:self
                                        action:@selector(doneClicked:)];
        
        UIBarButtonItem *flexibleSpace = [[UIBarButtonItem alloc] initWithBarButtonSystemItem:UIBarButtonSystemItemFlexibleSpace target:nil action:nil];
        [keyboardDoneButtonView setItems:[NSArray arrayWithObjects:flexibleSpace, flexibleSpace,doneButton, nil]];
    }
    else
    {
        keyboardDoneButtonView = nil;
    }
    
    UIReturnKeyType returnKeyType = UIReturnKeyDefault;
    NSString* returnKeyTypeString = [json getString:@"return_key_type"];
    if ([returnKeyTypeString isEqualToString:@"Next"])
    {
        returnKeyType = UIReturnKeyNext;
    }
    else if ([returnKeyTypeString isEqualToString:@"Done"])
    {
        returnKeyType = UIReturnKeyDone;
    }
    
    // Conversion for retina displays
    fontSize = fontSize / [UIScreen mainScreen].scale;
    
    UIFont* uiFont;
    if ([font length] > 0)
    {
        uiFont = [UIFont fontWithName:font size:fontSize];
    }
    else
    {
        uiFont = [UIFont systemFontOfSize:fontSize];
    }    
    
    if (multiline)
    {
        PlaceholderTextView* textView = [[PlaceholderTextView alloc] initWithFrame:CGRectMake(x, y, width, height)];
        textView.keyboardType = keyType;
        
        [textView setFont:uiFont];
        
        textView.scrollEnabled = TRUE;
        
        textView.delegate = self;
        textView.tag = 0;
        textView.text = @"";
        
        textView.textColor = textColor;
        textView.backgroundColor = backgroundColor;
        textView.returnKeyType = returnKeyType;
        textView.autocorrectionType = autoCorr ? UITextAutocorrectionTypeYes : UITextAutocorrectionTypeNo;
        textView.contentInset = UIEdgeInsetsMake(0.0f, 0.0f, 0.0f, 0.0f);
        textView.placeholder = placeholder;
        textView.placeholderColor = placeHolderColor;
        textView.delegate = self;
        if (keyType == UIKeyboardTypeEmailAddress)
            textView.autocapitalizationType = UITextAutocapitalizationTypeNone;
        
        [textView setSecureTextEntry:password];
        if (keyboardDoneButtonView != nil) textView.inputAccessoryView = keyboardDoneButtonView;
        
        
        /// Todo
        /// UITextView Alignment is not implemented
        
        editView = textView;
    }
    else
    {
        UITextField* textField = [[UITextField alloc] initWithFrame:CGRectMake(x, y, width, height)];
        textField.keyboardType = keyType;
        [textField setFont:uiFont];
        textField.delegate = self;
        textField.tag = 0;
        textField.text = @"";
        textField.textColor = textColor;
        textField.backgroundColor = backgroundColor;
        textField.returnKeyType = returnKeyType;
        textField.autocorrectionType = autoCorr ? UITextAutocorrectionTypeYes : UITextAutocorrectionTypeNo;
        textField.contentVerticalAlignment = valign;
        textField.contentHorizontalAlignment = halign;
        // Settings the placeholder like this is needed because otherwise it will not be visible
        textField.attributedPlaceholder = [[NSAttributedString alloc] initWithString:placeholder attributes:@{NSForegroundColorAttributeName: placeHolderColor}];
        textField.delegate = self;
        if (keyType == UIKeyboardTypeEmailAddress)
            textField.autocapitalizationType = UITextAutocapitalizationTypeNone;
        
        [textField addTarget:self action:@selector(textFieldDidChange:) forControlEvents:UIControlEventEditingChanged];
        [textField setSecureTextEntry:password];
        if (keyboardDoneButtonView != nil) textField.inputAccessoryView = keyboardDoneButtonView;
        
        editView = textField;
    }
    [viewPlugin addSubview:editView];
}

-(void) setText:(JsonObject*)json
{
    NSString* newText = [json getString:@"text"];
    if([editView isKindOfClass:[UITextField class]]) {
        [((UITextField*)editView) setText:newText];
    } else if([editView isKindOfClass:[UITextView class]]){
        [((UITextView*)editView) setText:newText];
    }
}

-(IBAction) doneClicked:(id)sender
{
    [self showKeyboard:false];
}

-(int) getLineCount
{
    if([editView isKindOfClass:[UITextField class]]) {
        return 1;
    } else if([editView isKindOfClass:[UITextView class]]){
        UITextView* tv = ((UITextView*)editView);
        int lineCount = (int) tv.contentSize.height / tv.font.lineHeight;
        return (lineCount);
    }
    return 0;
}


-(void) remove
{
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    [editView resignFirstResponder];
    [editView removeFromSuperview];
    if (keyboardDoneButtonView != nil)
    {
        doneButton = nil;
        keyboardDoneButtonView = nil;
    }
}

-(void) setFocus:(BOOL) isFocus
{
    if (isFocus)
    {
        [editView becomeFirstResponder];
    }
    else
    {
        [editView resignFirstResponder];
    }
}

-(NSString*) getText
{
    if([editView isKindOfClass:[UITextField class]]) {
        return ((UITextField*)editView).text;
    } else if([editView isKindOfClass:[UITextView class]]){
        return ((UITextView*)editView).text;
    }
    return @"";
}

-(bool) isFocused
{
    return editView.isFirstResponder;
}

-(void) showKeyboard:(bool)isShow
{
    [viewController.view endEditing:(isShow ? YES : NO)];
}
-(void) setVisible:(bool)isVisible
{
    editView.hidden = (isVisible ? NO : YES);
}

-(void) onTextChange:(NSString*) text
{
    JsonObject* jsonToUnity = [[JsonObject alloc] init];
    
    [jsonToUnity setString:@"msg" value:MSG_TEXT_CHANGE];
    [jsonToUnity setString:@"text" value:text];
    [self sendJsonToUnity:jsonToUnity];
}

-(void) onTextEditEnd:(NSString*) text
{
    JsonObject* jsonToUnity = [[JsonObject alloc] init];
    
    [jsonToUnity setString:@"msg" value:MSG_TEXT_END_EDIT];
    [jsonToUnity setString:@"text" value:text];
    [self sendJsonToUnity:jsonToUnity];
}

-(void) textViewDidChange:(UITextView *)textView
{
    [self onTextChange:textView.text];
}

-(void) textViewDidEndEditing:(UITextView *)textView
{
    [self onTextEditEnd:textView.text];
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField
{
    if (![editView isFirstResponder]) return YES;
    JsonObject* jsonToUnity = [[JsonObject alloc] init];
    
    [jsonToUnity setString:@"msg" value:MSG_RETURN_PRESSED];
    [self sendJsonToUnity:jsonToUnity];
    return YES;
}

- (BOOL)textField:(UITextField *)textField shouldChangeCharactersInRange:(NSRange)range replacementString:(NSString *)string {
    // Prevent crashing undo bug – see note below.
    if(range.length + range.location > textField.text.length)
    {
        return NO;
    }
    
    NSUInteger newLength = [textField.text length] + [string length] - range.length;
    if(characterLimit > 0)
        return newLength <= characterLimit;
    else
        return YES;
}

-(void) textFieldDidChange :(UITextField *)theTextField{
    [self onTextChange:theTextField.text];
}

-(void) keyboardWillShow:(NSNotification *)notification
{
    if (![editView isFirstResponder]) return;
    
    NSDictionary* keyboardInfo = [notification userInfo];
    NSValue* keyboardFrameBegin = [keyboardInfo valueForKey:UIKeyboardFrameBeginUserInfoKey];
    rectKeyboardFrame = [keyboardFrameBegin CGRectValue];

}

-(void) keyboardWillHide:(NSNotification*)notification
{
    if (![editView isFirstResponder]) return;

}

-(float) getKeyboardheight
{
    float height = rectKeyboardFrame.size.height / viewController.view.bounds.size.height;
    return height;
}


@end
